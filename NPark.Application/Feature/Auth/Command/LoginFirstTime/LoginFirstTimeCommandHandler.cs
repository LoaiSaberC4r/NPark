using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.Encryption;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Specifications.UserSpecification;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.Auth.Command.LoginFirstTime
{
    public class LoginFirstTimeCommandHandler : ICommandHandler<LoginFirstTimeCommand>
    {
        private readonly IGenericRepository<User> _repository;
        private readonly IPasswordService _passwordService;

        public LoginFirstTimeCommandHandler(IGenericRepository<User> repository, IPasswordService passwordService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        }

        public async Task<Result> Handle(LoginFirstTimeCommand request, CancellationToken cancellationToken)
        {
            var spec = new GetUserByUserName(request.UserName);

            User? user = await _repository.FirstOrDefaultWithSpecAsync(spec, cancellationToken);
            var matchPassword = _passwordService.Verify(request.Password, user.PasswordHash);
            if (!matchPassword)
                return Result.Fail(new Error
                    (ErrorMessage.WrongPassword, ErrorMessage.WrongPassword, ErrorType.Security));

            var newPassword = _passwordService.Hash(request.NewPassword);
            user.UpdatePasswordHash(newPassword);
            await _repository.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
    }
}