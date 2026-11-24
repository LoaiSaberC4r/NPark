using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetEntranceCashierTicketsReport
{
    public sealed record EntranceCashierTicketsReportResponse
    {
        public Guid UserId { get; init; }
        public string CashierName { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;

        public Guid RoleId { get; init; }
        public string RoleNameEn { get; init; } = string.Empty;
        public string RoleNameAr { get; init; } = string.Empty;

        public DateTime From { get; init; }
        public DateTime To { get; init; }

        public EntranceCashierTicketsReportSummaryResponse Summary { get; init; } = new();

        public IReadOnlyList<EntranceCashierInsideTicketResponse> InsideParkTickets { get; init; }
            = Array.Empty<EntranceCashierInsideTicketResponse>();

        public IReadOnlyList<EntranceCashierOutsideTicketResponse> OutsideParkTickets { get; init; }
            = Array.Empty<EntranceCashierOutsideTicketResponse>();
    }

    public sealed record EntranceCashierTicketsReportSummaryResponse
    {
        public int TotalEnteredTicketsCount { get; init; }

        public int PendingEntryTicketsCount { get; init; }
        public int AcknowledgedExitPendingArchiveCount { get; init; }
        public decimal ConfirmedBasePaidToCashier { get; init; }
        public int InsideParkTicketsCount { get; init; }
        public int OutsideParkTicketsCount { get; init; }

        public decimal InsideParkTotalBasePrice { get; init; }
        public decimal OutsideParkTotalBasePrice { get; init; }
        public decimal TotalBasePrice { get; init; }

        public decimal InsideParkTotalExceedPrice { get; init; }
        public decimal OutsideParkTotalExceedPrice { get; init; }
        public decimal TotalExceedPrice { get; init; }

        public decimal InsideParkTotalAmount { get; init; }
        public decimal OutsideParkTotalAmount { get; init; }
        public decimal TotalAmount { get; init; }

        public int InsideParkExceededTicketsCount { get; init; }
        public int OutsideParkExceededTicketsCount { get; init; }
        public int TotalExceededTicketsCount { get; init; }
    }

    public sealed record EntranceCashierInsideTicketResponse
    {
        public Guid TicketId { get; init; }
        public string UniqueCode { get; init; } = string.Empty;

        public DateTime StartDate { get; init; }

        public decimal Price { get; init; }
        public decimal ExceedPrice { get; init; }
        public decimal TotalPrice { get; init; }

        public string? VehicleNumber { get; init; }

        public bool IsSubscriber { get; init; }
        public string? SubscriberNationalId { get; init; }
        public string? CardNumber { get; init; }

        public Guid GateId { get; init; }
        public int GateNumber { get; init; }
        public IReadOnlyList<TicketAttachmentResponse> Attachments { get; init; }
    = Array.Empty<TicketAttachmentResponse>();
    }

    public sealed record EntranceCashierOutsideTicketResponse
    {
        public Guid TicketId { get; init; }
        public string UniqueCode { get; init; } = string.Empty;

        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }

        public decimal Price { get; init; }
        public decimal ExceedPrice { get; init; }
        public decimal TotalPrice { get; init; }

        public string? VehicleNumber { get; init; }

        public bool IsSubscriber { get; init; }
        public string? SubscriberNationalId { get; init; }
        public string? CardNumber { get; init; }

        public Guid GateId { get; init; }
        public int GateNumber { get; init; }

        public Guid ExitGateId { get; init; }
        public int ExitGateNumber { get; init; }

        public Guid? CollectedBy { get; init; }
        public string? CollectorName { get; init; }
        public DateTime? CollectedDate { get; init; }

        public IReadOnlyList<TicketArchiveAttachmentResponse> Attachments { get; init; }
            = Array.Empty<TicketArchiveAttachmentResponse>();
    }

    public sealed record TicketArchiveAttachmentResponse
    {
        public Guid AttachmentId { get; init; }
        public string FilePath { get; init; } = string.Empty;
    }
    public sealed record TicketAttachmentResponse
    {
        public Guid AttachmentId { get; init; }
        public string FilePath { get; init; } = string.Empty;
    }
}