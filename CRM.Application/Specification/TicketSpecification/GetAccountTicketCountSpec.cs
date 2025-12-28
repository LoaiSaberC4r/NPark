using BuildingBlock.Domain.Specification;
using CRM.Application.Feature.TicketManagement.Query.GetAccountTicketCount;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.TicketSpecification
{
    public sealed class GetAccountTicketCountSpec : Specification<Account, GetAccountTicketCountQueryResponse>
    {
        public GetAccountTicketCountSpec(Guid accountId)
        {
            AddCriteria(x => x.Id == accountId);
            Select(x => new GetAccountTicketCountQueryResponse
            {
                TicketCount = x.Sites.SelectMany(s => s.Tickets).Count()
            });
        }
    }
}