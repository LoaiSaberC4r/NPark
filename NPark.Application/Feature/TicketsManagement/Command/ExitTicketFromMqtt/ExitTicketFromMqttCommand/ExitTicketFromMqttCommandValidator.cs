using FluentValidation;

namespace NPark.Application.Feature.TicketsManagement.Command.ExitTicketFromMqtt.ExitTicketFromMqttCommand
{
    internal sealed class ExitTicketFromMqttCommandValidator
          : AbstractValidator<ExitTicketFromMqttCommand>
    {
        public ExitTicketFromMqttCommandValidator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage("Ticket id is required.");

            RuleFor(x => x.ControllerId)
                .NotEmpty()
                .WithMessage("Controller id is required.")
                .MaximumLength(50)
                .WithMessage("Controller id must not exceed 50 characters.")
                .Must(x => int.TryParse(x, out _))
                .WithMessage("Controller id must be numeric.");
        }
    }
}