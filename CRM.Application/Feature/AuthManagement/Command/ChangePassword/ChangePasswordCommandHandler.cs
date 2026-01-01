using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.Encryption;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Application.Abstraction.Security;
using CRM.Domain.Entities;
using CRM.Domain.Resources;
using Microsoft.AspNetCore.Http;

namespace CRM.Application.Feature.AuthManagement.Command.ChangePassword
{
    internal sealed record ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGenericRepository<SuperAdmin> _superAdminRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly ITokenReader _tokenReader;
        private readonly IPasswordService _passwordService;
        public ChangePasswordCommandHandler(IHttpContextAccessor httpContextAccessor,
            IGenericRepository<SuperAdmin> superAdminRepository,
            IGenericRepository<Account> accountRepository,
            ITokenReader tokenReader,
            IPasswordService passwordService)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _superAdminRepository = superAdminRepository ?? throw new ArgumentNullException(nameof(superAdminRepository));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _tokenReader = tokenReader ?? throw new ArgumentNullException(nameof(tokenReader));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        }
        public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var token = _httpContextAccessor?.HttpContext?.ReadToken(_tokenReader);
            if (token is null)
            {
                return Result.Fail(new Error(
                    Code: "user.NotFound",
                    Message: ErrorMessage.WrongLoginCredintial,
                    Type: ErrorType.Security
                    ));
            }
            var user = await _superAdminRepository.GetByIdAsync(token.UserId!.Value, cancellationToken);
            if (user is null)
            {
                //Check for Account

                var account = await _accountRepository.GetByIdAsync(token.UserId.Value, cancellationToken);
                if (account is null)
                {
                    return Result.Fail(new Error(
                            Code: "user.NotFound",
                            Message: ErrorMessage.WrongLoginCredintial,
                            Type: ErrorType.Security
                            ));
                }
                {
                    //2- Check Password
                    var accountresult = _passwordService.Verify(request.OldPassword, account.Password);
                    if (!accountresult)
                    {
                        return Result.Fail(new Error(
                            Code: "user.NotFound",
                            Message: ErrorMessage.WrongLoginCredintial,
                            Type: ErrorType.Security
                            ));
                    }
                    var newPassword = _passwordService.Hash(request.NewPassword);
                    account.ChangePassword(newPassword);
                    await _accountRepository.SaveChangesAsync(cancellationToken);
                    return Result.Ok();
                }
            }

            //2- Check Password
            var result = _passwordService.Verify(request.OldPassword, user.Password);
            if (!result)
            {
                return Result.Fail(new Error(
                    Code: "user.NotFound",
                    Message: ErrorMessage.WrongLoginCredintial,
                    Type: ErrorType.Security
                    ));
            }

            var newPassword2 = _passwordService.Hash(request.NewPassword);
            user.UpdatePassword(newPassword2);
            await _superAdminRepository.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
    }
}