using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.GetUncollectedEnterTickets
{
    public sealed class UncollectedEnterTicketsByGateSpec : Specification<Ticket>
    {
        public UncollectedEnterTicketsByGateSpec(Guid gateId)
        {
            AddCriteria(t =>
                t.GateId == gateId &&
                t.CollectedBy == null &&
                t.IsCollected == false);
        }
    }
}