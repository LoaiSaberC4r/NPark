namespace CRM.Application.Feature.TicketManagement.Query.GetAccountTicketCount
{
    public sealed record GetAccountTicketCountQueryResponse
    {
        public int TicketCount { get; init; }
    }
}