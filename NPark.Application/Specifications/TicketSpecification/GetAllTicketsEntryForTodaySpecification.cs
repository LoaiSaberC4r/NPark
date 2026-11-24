using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.TicketsManagement.Query.GetEntryTicketForAdmin;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.TicketSpecification
{
    public sealed class GetAllTicketsEntryForTodaySpecification : Specification<Ticket, GetEntryTicketForAdminQueryResponse>
    {
        public GetAllTicketsEntryForTodaySpecification(DateTime start)
        {
            var end = start.AddDays(1);
            AddCriteria(t => t.StartDate >= start && t.StartDate < end);
            UseNoTracking();
            Select(x => new GetEntryTicketForAdminQueryResponse
            {
                Id = x.Id,
                isCollectedByCashier = x.IsCashierCollected,
                Price = x.Price,
                TicketNumber = x.UniqueCode,
                StartDate = x.StartDate,
                TicketInfo = x.VehicleNumber
            });
        }
    }
}