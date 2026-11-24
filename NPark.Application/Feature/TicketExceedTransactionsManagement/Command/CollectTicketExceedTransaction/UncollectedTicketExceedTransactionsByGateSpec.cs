using NPark.Domain.Enums;
using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction
{
    public sealed class UncollectedTicketExceedTransactionsByGateSpec
      : Specification<TicketExceedTransaction>
    {
        public UncollectedTicketExceedTransactionsByGateSpec(Guid gateId)
        {
            Include(x => x.UserCollector);

            AddCriteria(x =>
                (x.PaymentGateId ?? x.GateId) == gateId && x.PaymentStatus != SanctionPaymentStatus.Assessed && x.ExceedAmount > 0 &&
                x.SuperVisorCollectorId == null);

            AddOrderBy(x => x.CreatedAt);
        }
    }
}