using FluentValidation;

namespace NPark.Application.Feature.GateManagement.Command.OpenGate
{
    internal sealed class OpenGateCommandValidator : AbstractValidator<OpenGateCommand>
    {
        public OpenGateCommandValidator()
        {
            RuleFor(x => x.ControllerId)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}