using CRM.Domain.Resources;
using FluentValidation;

namespace CRM.Application.Feature.AuthManagement.Command.ChangePassword
{
    internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        // Optional complexity: upper + lower + digit + special, 8..128
        private const string PasswordRegex =
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,128}$";

        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage(ErrorMessage.ChangePassword_OldPassword_Required);

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(ErrorMessage.ChangePassword_NewPassword_Required)
                .MinimumLength(8).WithMessage(ErrorMessage.ChangePassword_NewPassword_MinLength)
                .MaximumLength(128).WithMessage(ErrorMessage.ChangePassword_NewPassword_MaxLength)
                .NotEqual(x => x.OldPassword).WithMessage(ErrorMessage.ChangePassword_NewPassword_SameAsOld)
                .Matches(PasswordRegex).WithMessage(ErrorMessage.ChangePassword_NewPassword_Invalid);
        }
    }
}