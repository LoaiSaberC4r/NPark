using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.TicketSpecification
{
    public sealed class TotalArchivedTicketsForTodayForExitSpec : Specification<TicketArchive>
    {
        public TotalArchivedTicketsForTodayForExitSpec(Guid gateId)
        {
            var today = DateTime.Now.Date;
            var start = today;
            var end = today.AddDays(1);

            AddCriteria(t =>
                t.EndDate >= start &&
                t.EndDate < end &&
                t.ExitGateId == gateId &&
                !t.IsCollected);

            EnableTotalCount();
        }
    }
}
