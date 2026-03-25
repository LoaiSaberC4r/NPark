using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.TicketSpecification
{
    public sealed class TotalArchivedTicketsForTodaySpec : Specification<TicketArchive>
    {
        public TotalArchivedTicketsForTodaySpec(Guid gateId)
        {
            var today = DateTime.Now.Date;
            var start = today;
            var end = today.AddDays(1);

            AddCriteria(t =>
                t.StartDate >= start &&
                t.StartDate < end &&
                t.GateId == gateId &&
                !t.IsCollected);

            EnableTotalCount();
        }
    }
}
