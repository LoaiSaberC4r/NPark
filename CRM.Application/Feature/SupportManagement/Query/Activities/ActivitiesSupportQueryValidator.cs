using BuildingBlock.Application.Repositories;
using CRM.Domain.Entities;
using CRM.Domain.Resources;
using FluentValidation;

namespace CRM.Application.Feature.SupportManagement.Query.Activities
{
    internal sealed class ActivitiesSupportQueryValidator : AbstractValidator<ActivitiesSupportQuery>
    {
        private readonly IGenericRepository<Supporter> _ticketRepository;

        public ActivitiesSupportQueryValidator(IGenericRepository<Supporter> ticketRepository)
        {
            _ticketRepository = ticketRepository;

            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage(ErrorMessage.SupportActivities_Id_Required)
                .WithErrorCode("SupportTicket.GetActivities");

            // Ticket must be found
            RuleFor(x => x.Id)
                .MustAsync(TicketExistsAsync)
                .WithMessage(ErrorMessage.SupportActivities_Id_NotFound)
                .WithErrorCode("SupportTicket.NotFound");
        }

        private async Task<bool> TicketExistsAsync(Guid ticketId, CancellationToken ct)
            => await _ticketRepository.IsExistAsync(x => x.Id == ticketId, ct);
    }
}