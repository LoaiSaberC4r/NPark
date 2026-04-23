using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText
{
    internal sealed class ValidateExitByTextCommandValidator : AbstractValidator<ValidateExitByTextCommand>
    {
        public ValidateExitByTextCommandValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty()
                .WithMessage(ErrorMessage.QrCode_Require)
                .MaximumLength(200);
        }
    }
}