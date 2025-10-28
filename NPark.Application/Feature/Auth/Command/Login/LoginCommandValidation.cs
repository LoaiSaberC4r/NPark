using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.Auth.Command.Login
{
    public sealed class LoginCommandValidation : AbstractValidator<LoginCommand>
    {
        private readonly IGenericRepository<User> _user;

        public LoginCommandValidation(IGenericRepository<User> user)
        {
            _user = user;
            RuleFor(x => x.UserName).NotEmpty().WithMessage(ErrorMessage.IsRequired)
    .MustAsync(async (username, cancellation) =>
    await _user.IsExistAsync(x => x.Username == username)).WithMessage(ErrorMessage.NotFound);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(ErrorMessage.PasswordRequired);
        }
    }
}