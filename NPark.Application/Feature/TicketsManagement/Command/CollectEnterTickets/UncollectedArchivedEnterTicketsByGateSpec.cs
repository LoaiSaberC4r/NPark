using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Command.CollectEnterTickets
{
    public sealed class UncollectedArchivedEnterTicketsByGateSpec : Specification<TicketArchive>
    {
        public UncollectedArchivedEnterTicketsByGateSpec(Guid gateId)
        {
            AddCriteria(t =>
                t.GateId == gateId &&
                t.CollectedBy == null &&
                t.IsCollected == false);
        }
    }
}