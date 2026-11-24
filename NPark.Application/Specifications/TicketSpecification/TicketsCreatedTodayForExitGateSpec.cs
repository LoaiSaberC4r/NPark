using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.TicketsManagement.Query.GetTicketForEntryGate;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.TicketSpecification
{
    internal class TicketsCreatedTodayForExitGateSpec : Specification<TicketArchive, GetTicketInfo>
    {
        public TicketsCreatedTodayForExitGateSpec(DateTime start)
        {
            var end = start.AddDays(1);
            AddCriteria(t => t.EndDate >= start && t.EndDate < end);
            AddCriteria(x => !x.IsCollected);
            UseNoTracking();
            Select(x => new GetTicketInfo
            {
                isCollectedByCashier = x.IsCashierCollected,
                Price = x.Price,
                TicketNumber = x.UniqueCode,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                TicketId = x.Id,
                TicketInfo = x.VehicleNumber
            });
        }
    }
}