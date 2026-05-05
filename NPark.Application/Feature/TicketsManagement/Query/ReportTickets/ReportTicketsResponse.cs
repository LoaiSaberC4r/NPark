namespace NPark.Application.Feature.TicketsManagement.Query.ReportTickets
{
    public sealed record class ReportTicketsResponse
    {
        public Guid TicketId { get; init; }

        public DateTime EnterDate { get; init; }

        public DateTime? ExitDate { get; init; }

        public string? VehicleNumber { get; init; }

        public bool IsSubscribed { get; init; }

        public decimal Price { get; init; }

        public string CollectorName { get; init; } = string.Empty;

        public string TicketNumber { get; init; } = string.Empty;

        public bool IsInsideParking { get; init; }

        public string TicketStatus { get; init; } = string.Empty;

        public Guid CreatedByUserId { get; init; }

        public string CreatedByUserName { get; init; } = string.Empty;

        public int? EntryGateNumber { get; init; }

        public int? ExitGateNumber { get; init; }
        public decimal? ExceedPrice { get; init; }

        public IReadOnlyCollection<ReportTicketAttachmentResponse> Attachments { get; init; }
            = Array.Empty<ReportTicketAttachmentResponse>();
    }

    public sealed record class ReportTicketAttachmentResponse
    {
        public string ImagePath { get; init; } = string.Empty;
    }
}