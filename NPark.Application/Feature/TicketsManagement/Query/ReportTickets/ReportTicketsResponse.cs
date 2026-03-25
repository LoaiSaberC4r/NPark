namespace NPark.Application.Feature.TicketsManagement.Query.ReportTickets
{
    public sealed record class ReportTicketsResponse
    {
        public Guid TicketId { get; init; }
        public DateTime EnterDate { get; init; }
        public DateTime ExitDate { get; init; }
        public string? VehicleNumber { get; init; }
        public bool IsSubscribed { get; init; }
        public decimal Price { get; init; }
        public string CollectorName { get; init; } = string.Empty;
        public string TicketNumber { get; init; } = string.Empty;
    }
}