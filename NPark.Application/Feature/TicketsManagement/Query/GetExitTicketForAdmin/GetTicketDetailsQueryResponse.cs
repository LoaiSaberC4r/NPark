namespace NPark.Application.Feature.TicketsManagement.Query.GetExitTicketForAdmin
{
    public sealed record GetTicketDetailsQueryResponse
    {
        public Guid TicketId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public decimal Price { get; set; }
        public decimal ExceedPrice { get; set; }
        public decimal TotalPrice { get; set; }

        public string? VehicleNumber { get; set; }

        public bool IsCollected { get; set; }
        public Guid? CollectedBy { get; set; }
        public DateTime? CollectedDate { get; set; }

        public string UniqueCode { get; set; } = string.Empty;

        public Guid GateId { get; set; }
        public Guid? ExitGateId { get; set; }
        public Guid UserId { get; set; }

        public bool IsCashierCollected { get; set; }

        public bool IsSubscriber { get; set; }
        public string? SubscriberNationalId { get; set; }
        public string? CardNumber { get; set; }

        public List<TicketAttachmentDetailsResponse> Attachments { get; set; } = new();
    }

    public sealed record TicketAttachmentDetailsResponse
    {
        public Guid AttachmentId { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }
}