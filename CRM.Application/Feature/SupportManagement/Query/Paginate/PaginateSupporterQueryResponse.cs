namespace CRM.Application.Feature.SupportManagement.Query.Paginate
{
    public sealed record class PaginateSupporterQueryResponse
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public int AssignedTicketCount { get; init; }
        public int ClosedTicketCount { get; init; }
    }
}