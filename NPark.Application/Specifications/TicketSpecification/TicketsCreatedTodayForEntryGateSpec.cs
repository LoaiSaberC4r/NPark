using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.TicketsManagement.Query.GetTicketForEntryGate;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.TicketSpecification
{
    public class TicketsCreatedTodayForEntryGateSpec : Specification<Ticket, GetTicketInfo>
    {
        public TicketsCreatedTodayForEntryGateSpec(Guid gateId, DateTime start)
        {
            var end = start.AddDays(1);
            AddCriteria(t => t.StartDate >= start && t.StartDate < end && t.GateId == gateId);
            AddCriteria(x => !x.IsCollected);
            UseNoTracking();
            Select(x => new GetTicketInfo
            {
                isCollectedByCashier = x.IsCashierCollected,
                Price = x.Price,
                TicketNumber = x.UniqueCode,
                StartDate = x.StartDate,
                TicketId = x.Id,
                EntryConfirmedAtUtc = x.EntryConfirmedAtUtc,
                EntryConfirmationStatus = x.EntryConfirmationStatus,
                TicketInfo = x.VehicleNumber
            });
        }
    }
}