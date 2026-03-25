using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.Encryption;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Abstraction.Security;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.UserManagement.Command.Update
{
    public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
    {
        private readonly IGenericRepository<User> _userRepository;
        private readonly IAuditLogger _auditLogger;
        private static readonly Guid AdminRoleId = new Guid("f47ac10b-58cc-4372-a567-0e02b2c3d479");
        private readonly IPasswordService _passwordService;

        public UpdateUserCommandHandler(IGenericRepository<User> userRepository, IAuditLogger auditLogger, IPasswordService passwordService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _auditLogger = auditLogger;
            _passwordService = passwordService;
        }

        public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var entity = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result.Fail(new Error(
                    Code: "User.NotFound",
                    Message: ErrorMessage.UserManagement_Id_NotFound,
                    Type: ErrorType.NotFound));
            }

            if (entity.RoleId == AdminRoleId && request.RoleId != AdminRoleId)
            {
                return Result.Fail(new Error(
                    Code: "User.UpdateForbidden",
                    Message: ErrorMessage.UserManagement_RoleId_AdminImmutable,
                    Type: ErrorType.Security));
            }
            entity!.UpdateEmail(request.Email);
            entity.UpdateUserName(request.UserName);
            entity.SetRole(request.RoleId);
            if (!string.IsNullOrWhiteSpace(request.Password))

            {
                var hashedPassword = _passwordService.Hash(request.Password);
                entity.UpdatePasswordHash(hashedPassword);
            }
            await _userRepository.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                    new AuditLogEntry(
                        EventName: "UpdateUser",
                        EventCategory: "UserManagement",
                        IsSuccess: true,
                        StatusCode: 200,
                        UserId: entity.Id,
                        Extra: new
                        {
                            entity.Name,
                            entity.Email,
                            entity.Username,
                            entity.Role
                        }),
                    cancellationToken);

            return Result.Ok();
        }
    }
}