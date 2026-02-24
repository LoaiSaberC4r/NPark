using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.Encryption;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Application.Abstraction.Security;
using CRM.Application.Specification.AccountSpecification;
using CRM.Application.Specification.AuthSpecification;
using CRM.Application.Specification.SupportSpecification;
using CRM.Domain.Entities;
using CRM.Domain.Resources;
using Microsoft.Extensions.Logging;

namespace CRM.Application.Feature.AuthManagement.Command.Login
{
    internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponseCommand>
    {
        private readonly IGenericRepository<SuperAdmin> _superAdminRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<Supporter> _supportrepository;
        private readonly ILogger<LoginCommandHandler> _logger;
        private readonly IJwtProvider _jwtProvider;
        private readonly IPasswordService _passwordService;

        public LoginCommandHandler(IGenericRepository<SuperAdmin> superAdminRepository, ILogger<LoginCommandHandler> logger,
            IPasswordService passwordService, IJwtProvider jwtProvider, IGenericRepository<Account> accountRepository,
            IGenericRepository<Supporter> supportrepository)
        {
            _superAdminRepository = superAdminRepository ?? throw new ArgumentNullException(nameof(superAdminRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtProvider = jwtProvider ?? throw new ArgumentNullException(nameof(jwtProvider));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _supportrepository = supportrepository;
        }

        public async Task<Result<LoginResponseCommand>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            //1- Get User by Email
            var spec = new LoginSpec(request.Email);
            var user = await _superAdminRepository.FirstOrDefaultWithSpecAsync(spec, cancellationToken);
            if (user is null)
            {
                //Check for Account
                var accountSpec = new GetAccountByEmailSpec(request.Email);
                var account = await _accountRepository.FirstOrDefaultWithSpecAsync(accountSpec, cancellationToken);
                if (account is null)
                {
                    //Supporter
                    var supportSpec = new GetSupporterByEmailSpec(request.Email);
                    var supporter = await _supportrepository.FirstOrDefaultWithSpecAsync(supportSpec, cancellationToken);
                    if (supporter is null)
                    {
                        return Result<LoginResponseCommand>.Fail(new Error(
                            Code: "user.NotFound",
                            Message: ErrorMessage.WrongLoginCredintial,
                            Type: ErrorType.Security
                            ));
                    }
                    var supportresult = _passwordService.Verify(request.Password, supporter.Password);
                    if (!supportresult)
                    {
                        return Result<LoginResponseCommand>.Fail(new Error(
                            Code: "user.NotFound",
                            Message: ErrorMessage.WrongLoginCredintial,
                            Type: ErrorType.Security
                            ));
                    }
                    var supporterToken = await _jwtProvider.GenerateSupporter(supporter, cancellationToken);
                    return Result<LoginResponseCommand>.Ok(new LoginResponseCommand
                    { TokenInfo = supporterToken, IsFirstLogin = supporter.IsFirstLogin });
                }
                {
                    //2- Check Password
                    var accountresult = _passwordService.Verify(request.Password, account.Password);
                    if (!accountresult)
                    {
                        return Result<LoginResponseCommand>.Fail(new Error(
        Code: "user.NotFound",
        Message: ErrorMessage.WrongLoginCredintial,
        Type: ErrorType.Security
        ));
                    }
                    var accounttoken = await _jwtProvider.GenerateAccount(account, cancellationToken);
                    return Result<LoginResponseCommand>.Ok(new LoginResponseCommand { TokenInfo = accounttoken, IsFirstLogin = account.IsFirstLogin ?? true });
                }
            }

            //2- Check Password
            var result = _passwordService.Verify(request.Password, user.Password);
            if (!result)
            {
                _logger.LogInformation("User {Email} found in database", user.Email);

                return Result<LoginResponseCommand>.Fail(new Error(
                    Code: "user.NotFound",
                    Message: ErrorMessage.WrongLoginCredintial,
                    Type: ErrorType.Security
                    ));
            }

            //3- Generate Token
            var token = await _jwtProvider.Generate(user, cancellationToken);
            _logger.LogInformation("Login by {Email} is  Success", user.Email);
            return Result<LoginResponseCommand>.Ok(new LoginResponseCommand { TokenInfo = token, IsFirstLogin = user.IsFirstLogin });
        }
    }
}