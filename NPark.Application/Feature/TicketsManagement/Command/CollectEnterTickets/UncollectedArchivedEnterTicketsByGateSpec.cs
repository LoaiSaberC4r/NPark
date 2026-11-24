using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Application.Feature.TicketsManagement.Command.CollectEnterTickets
{
    public sealed class UncollectedArchivedEnterTicketsByGateSpec : Specification<TicketArchive>
    {
        public UncollectedArchivedEnterTicketsByGateSpec(Guid gateId)
        {
            Include(t => t.User);

            AddCriteria(t =>
                (t.BasePaymentGateId ?? t.GateId) == gateId &&
                (t.PricingModeSnapshot == null || t.PricingModeSnapshot == PriceType.Enter) &&
                t.IsCashierCollected && t.Price > 0 &&
                t.CollectedBy == null &&
                t.IsCollected == false);
        }
    }
}