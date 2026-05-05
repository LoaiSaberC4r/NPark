using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.Encryption;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Application.Abstraction.Security;
using NPark.Application.Shared.Dto;
using NPark.Application.Specifications.UserSpecification;
using NPark.Domain.Entities;
using NPark.Domain.FileNames;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.Auth.Command.Login
{
    public sealed class LoginCommandHandler
        : ICommandHandler<LoginCommand, UserTokenDto>
    {
        private readonly IGenericRepository<User> _userRepository;
        private readonly IJwtProvider _jwtProvider;
        private readonly IPasswordService _passwordService;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IGenericRepository<User> userRepository,
            IJwtProvider jwtProvider,
            IPasswordService passwordService,
            IAuditLogger auditLogger,
            ILogger<LoginCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _jwtProvider = jwtProvider ?? throw new ArgumentNullException(nameof(jwtProvider));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<UserTokenDto>> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // 1) Get user by username
                var spec = new GetUserByUserName(request.UserName);

                var userEntity = await _userRepository
                    .FirstOrDefaultWithSpecAsync(spec, cancellationToken);

                // 2) Validate username/password
                var credentialsValid =
                    userEntity is not null &&
                    _passwordService.Verify(request.Password, userEntity.PasswordHash);

                if (!credentialsValid)
                {
                    _logger.LogWarning(
                        "Invalid login attempt for username: {UserName}",
                        request.UserName);

                    await SafeAuditLoginFailedAsync(
                        userEntity?.Id,
                        request.UserName,
                        cancellationToken);

                    return Result<UserTokenDto>.Fail(
                        new Error(
                            Code: "Auth.InvalidCredentials",
                            Message: ErrorMessage.Auth_InvalidCredentials,
                            Type: ErrorType.Security));
                }

                // 3) Validate cashier shift before generating token
                var shiftError = ValidateCashierShift(userEntity!);

                if (shiftError is not null)
                {
                    _logger.LogWarning(
                        "Login blocked outside shift. UserId: {UserId}, UserName: {UserName}, RoleId: {RoleId}, ShiftStart: {ShiftStart}, ShiftEnd: {ShiftEnd}, CurrentTime: {CurrentTime}",
                        userEntity!.Id,
                        request.UserName,
                        userEntity.RoleId,
                        userEntity.ShiftStartTime,
                        userEntity.ShiftEndTime,
                        DateTime.Now.TimeOfDay);

                    await SafeAuditLoginOutsideShiftAsync(
                        userEntity.Id,
                        request.UserName,
                        shiftError.Code,
                        shiftError.Message,
                        cancellationToken);

                    return Result<UserTokenDto>.Fail(shiftError);
                }

                // 4) Generate JWT token
                var tokenDto = await _jwtProvider.Generate(userEntity!);

                // 5) Audit success
                await SafeAuditLoginSucceededAsync(
                    userEntity!.Id,
                    request.UserName,
                    cancellationToken);

                _logger.LogInformation(
                    "User logged in successfully. UserId: {UserId}, UserName: {UserName}",
                    userEntity.Id,
                    request.UserName);

                return Result<UserTokenDto>.Ok(tokenDto);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Login operation was canceled for username: {UserName}",
                    request.UserName);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while processing login for username: {UserName}",
                    request.UserName);

                return Result<UserTokenDto>.Fail(
                    new Error(
                        Code: "Auth.Login.Unexpected",
                        Message: ErrorMessage.AuthLogin_Unexpected,
                        Type: ErrorType.Infrastructure));
            }
        }

        private static Error? ValidateCashierShift(User user)
        {
            // RoleId is required now, so no nullable check needed.
            if (!AppRoleIds.IsCashier(user.RoleId))
            {
                return null;
            }

            if (user.ShiftStartTime is null || user.ShiftEndTime is null)
            {
                return new Error(
                    Code: "Auth.ShiftNotConfigured",
                    Message: "Shift is not configured for this cashier user. / لم يتم تحديد شيفت لهذا الكاشير.",
                    Type: ErrorType.Security);
            }

            var currentTime = DateTime.Now.TimeOfDay;

            var isInsideShift = IsCurrentTimeInsideShift(
                currentTime,
                user.ShiftStartTime.Value,
                user.ShiftEndTime.Value);

            if (!isInsideShift)
            {
                return new Error(
                    Code: "Auth.LoginOutsideShift",
                    Message: "You are not allowed to login outside your shift time. / غير مسموح بتسجيل الدخول خارج وقت الشيفت.",
                    Type: ErrorType.Security);
            }

            return null;
        }

        private static bool IsCurrentTimeInsideShift(
            TimeSpan currentTime,
            TimeSpan shiftStartTime,
            TimeSpan shiftEndTime)
        {
            // Invalid shift configuration.
            if (shiftStartTime == shiftEndTime)
            {
                return false;
            }

            // Normal shift example:
            // 08:00 -> 16:00
            if (shiftStartTime < shiftEndTime)
            {
                return currentTime >= shiftStartTime &&
                       currentTime <= shiftEndTime;
            }

            // Night shift example:
            // 22:00 -> 06:00
            return currentTime >= shiftStartTime ||
                   currentTime <= shiftEndTime;
        }

        private async Task SafeAuditLoginFailedAsync(
            Guid? userId,
            string userName,
            CancellationToken cancellationToken)
        {
            try
            {
                await _auditLogger.LogAsync(
                    new AuditLogEntry(
                        EventName: "LoginFailed",
                        EventCategory: "Auth",
                        IsSuccess: false,
                        StatusCode: StatusCodes.Status401Unauthorized,
                        ErrorCode: "Auth.InvalidCredentials",
                        ErrorMessage: "اسم المستخدم أو كلمة المرور غير صحيحة (Invalid username or password).",
                        UserId: userId,
                        Extra: new
                        {
                            UserName = userName
                        }),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Audit logging failed for LoginFailed. UserName: {UserName}",
                    userName);
            }
        }

        private async Task SafeAuditLoginOutsideShiftAsync(
            Guid userId,
            string userName,
            string errorCode,
            string errorMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                await _auditLogger.LogAsync(
                    new AuditLogEntry(
                        EventName: "LoginOutsideShift",
                        EventCategory: "Auth",
                        IsSuccess: false,
                        StatusCode: StatusCodes.Status403Forbidden,
                        ErrorCode: errorCode,
                        ErrorMessage: errorMessage,
                        UserId: userId,
                        Extra: new
                        {
                            UserName = userName
                        }),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Audit logging failed for LoginOutsideShift. UserId: {UserId}, UserName: {UserName}",
                    userId,
                    userName);
            }
        }

        private async Task SafeAuditLoginSucceededAsync(
            Guid userId,
            string userName,
            CancellationToken cancellationToken)
        {
            try
            {
                await _auditLogger.LogAsync(
                    new AuditLogEntry(
                        EventName: "LoginSucceeded",
                        EventCategory: "Auth",
                        IsSuccess: true,
                        StatusCode: StatusCodes.Status200OK,
                        UserId: userId,
                        Extra: new
                        {
                            UserName = userName
                        }),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Audit logging failed for LoginSucceeded. UserId: {UserId}, UserName: {UserName}",
                    userId,
                    userName);
            }
        }
    }
}