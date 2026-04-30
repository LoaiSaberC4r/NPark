using FluentValidation;

namespace NPark.Application.Feature.GateManagement.Command.OpenGate
{
    internal sealed class OpenGateCommandValidator : AbstractValidator<OpenGateCommand>
    {
        public OpenGateCommandValidator()
        {
            RuleFor(x => x.ControllerId)
                .NotEmpty()
                .WithMessage("Controller id is required.")
                .MaximumLength(100)
                .WithMessage("Controller id must not exceed 100 characters.")
                .Must(x => int.TryParse(x, out _))
                .WithMessage("Controller id must be numeric.");
        }
    }
}