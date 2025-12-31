using CRM.Domain.Resources;
using FluentValidation;

namespace CRM.Application.Feature.SupportManagement.Command.Add
{
    internal sealed class AddSupportCommandValidator : AbstractValidator<AddSupportCommand>
    {
        public AddSupportCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(ErrorMessage.Login_Email_Required)
                .EmailAddress().WithMessage(ErrorMessage.Login_Email_Invalid);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(ErrorMessage.Login_Password_Required)
                .MinimumLength(8).WithMessage(ErrorMessage.Login_Password_MinLength)
                .MaximumLength(128).WithMessage(ErrorMessage.Login_Password_MaxLength);

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^(?:\+20|0020|0)?1[0125]\d{8}$")
                .WithMessage(ErrorMessage.Ticket_Phone_Invalid)
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        }
    }
}