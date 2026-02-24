using BuildingBlock.Application.Repositories;
using CRM.Domain.Entities;
using CRM.Domain.Resources;
using FluentValidation;

namespace CRM.Application.Feature.SupportManagement.Command.Reject
{
    internal sealed class RejectSupporterCommandValidator : AbstractValidator<RejectSupporterCommand>
    {
        private const int RejectReasonMaxLength = 500;
        private readonly IGenericRepository<Ticket> _ticketRepository;

        public RejectSupporterCommandValidator(IGenericRepository<Ticket> ticketRepository)
        {
            _ticketRepository = ticketRepository;
            RuleLevelCascadeMode = CascadeMode.Stop;
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.TicketId)
                .NotEmpty()
                .WithMessage(ErrorMessage.Support_TicketId_Required)
                .WithErrorCode("Ticket.Reject");

            RuleFor(x => x.RejectReason)
                .NotEmpty()
                .WithMessage(ErrorMessage.Support_RejectReason_Required)
                .WithErrorCode("Ticket.Reject")
                .MaximumLength(RejectReasonMaxLength)
                .WithMessage(ErrorMessage.Support_RejectReason_MaxLength)
                .WithErrorCode("Ticket.Reject");

            // Ticket must exist in DB
            When(x => x.TicketId != Guid.Empty, () =>
            {
                RuleFor(x => x.TicketId)
                    .MustAsync(TicketExistsAsync)
                    .WithMessage(ErrorMessage.Support_TicketId_NotFound)
                    .WithErrorCode("Ticket.NotFound");
            });
        }

        private async Task<bool> TicketExistsAsync(Guid ticketId, CancellationToken ct)
        {
            // assumes repository supports AnyAsync(predicate, ct)
            return await _ticketRepository.IsExistAsync(t => t.Id == ticketId, ct);
        }
    }
}