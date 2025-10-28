using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.Auth.Command.LoginFirstTime
{
    public class LoginFirstTimeCommandValidation : AbstractValidator<LoginFirstTimeCommand>
    {
        private readonly IGenericRepository<User> _user;

        public LoginFirstTimeCommandValidation(IGenericRepository<User> user)
        {
            _user = user;

            RuleFor(x => x.UserName).NotEmpty().WithMessage(ErrorMessage.IsRequired)
                .MustAsync(async (username, cancellation) =>
                await _user.IsExistAsync(x => x.Username == username)).WithMessage(ErrorMessage.NotFound);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(ErrorMessage.PasswordRequired);
            RuleFor(x => x.NewPassword)
    .NotEmpty().WithMessage(ErrorMessage.PasswordRequired);

            RuleFor(x => x.ConfirmedPassword)
    .NotEmpty().WithMessage(ErrorMessage.PasswordRequired);

            RuleFor(x => x.ConfirmedPassword)
                   .Equal(x => x.NewPassword)
                   .WithMessage(ErrorMessage.PasswordMismatch);
        }
    }
}