using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.TicketsManagement.Query.GetExitTicketForAdmin;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.TicketSpecification
{
    public sealed class GetAllTicketsExitForTodaySpecification : Specification<TicketArchive, GetExitTicketForAdminQueryResponse>
    {
        public GetAllTicketsExitForTodaySpecification(DateTime start)
        {
            var end = start.AddDays(1);
            AddCriteria(t => t.EndDate >= start && t.EndDate < end);
            UseNoTracking();
            Select(x => new GetExitTicketForAdminQueryResponse
            {
                Id = x.Id,
                isCollectedByCashier = x.IsCashierCollected,
                Price = x.Price,
                TicketNumber = x.UniqueCode,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                TicketInfo = x.VehicleNumber
            });
        }
    }
}