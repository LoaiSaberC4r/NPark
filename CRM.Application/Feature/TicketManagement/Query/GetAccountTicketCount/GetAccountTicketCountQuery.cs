using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.TicketManagement.Query.GetAccountTicketCount
{
    public sealed record GetAccountTicketCountQuery : IQuery<GetAccountTicketCountQueryResponse>
    {
    }
}