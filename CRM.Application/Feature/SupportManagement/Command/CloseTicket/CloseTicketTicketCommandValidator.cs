using CRM.Domain.Resources;
using FluentValidation;

namespace CRM.Application.Feature.SupportManagement.Command.CloseTicket
{
    internal sealed class CloseTicketTicketCommandValidator : AbstractValidator<CloseTicketTicketCommand>
    {
        public CloseTicketTicketCommandValidator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty().WithMessage(ErrorMessage.Ticket_Id_Required);

            When(x => x.Description is not null, () =>
            {
                RuleFor(x => x.Description!)
                    .Must(d => !string.IsNullOrWhiteSpace(d))
                    .WithMessage(ErrorMessage.CloseTicket_Description_Empty);

                RuleFor(x => x.Description!)
                    .MaximumLength(2000)
                    .WithMessage(ErrorMessage.CloseTicket_Description_MaxLength);
            });
        }
    }
}