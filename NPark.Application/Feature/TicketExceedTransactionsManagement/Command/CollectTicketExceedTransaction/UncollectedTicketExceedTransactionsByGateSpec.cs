using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction
{
    public sealed class UncollectedTicketExceedTransactionsByGateSpec
      : Specification<TicketExceedTransaction>
    {
        public UncollectedTicketExceedTransactionsByGateSpec(Guid gateId)
        {
            AddCriteria(x =>
                x.GateId == gateId &&
                x.SuperVisorCollectorId == null);

            AddOrderBy(x => x.CreatedAt);
        }
    }
}