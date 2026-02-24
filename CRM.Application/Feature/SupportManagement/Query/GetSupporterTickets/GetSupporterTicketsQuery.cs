using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.SupportManagement.Query.GetSupporterTickets
{
    public sealed record GetSupporterTicketsQuery : IQuery<GetSupporterTicketsQueryResponse>
    {
    }
}