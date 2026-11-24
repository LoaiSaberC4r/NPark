using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport
{
    public sealed record SupervisorCollectionsReportResponse
    {
        public SupervisorReportInfoResponse Supervisor { get; init; } = new();

        public SupervisorCollectionsPeriodResponse Period { get; init; } = new();

        public SupervisorCollectionsSummaryResponse Summary { get; init; } = new();

        public IReadOnlyList<TicketCollectorSummaryResponse> TicketCollectorsSummary { get; init; }
            = Array.Empty<TicketCollectorSummaryResponse>();

        public IReadOnlyList<ActiveTicketCollectionResponse> ActiveTicketCollections { get; init; }
            = Array.Empty<ActiveTicketCollectionResponse>();

        public IReadOnlyList<ArchivedTicketCollectionResponse> ArchivedTicketCollections { get; init; }
            = Array.Empty<ArchivedTicketCollectionResponse>();

        public IReadOnlyList<SupervisorExceedCollectionResponse> SupervisorExceedCollections { get; init; }
            = Array.Empty<SupervisorExceedCollectionResponse>();

        public IReadOnlyList<PendingExceedCollectionResponse> PendingExceedCollections { get; init; }
            = Array.Empty<PendingExceedCollectionResponse>();
    }

    public sealed record SupervisorReportInfoResponse
    {
        public Guid SupervisorId { get; init; }

        public string SupervisorName { get; init; } = string.Empty;

        public string Username { get; init; } = string.Empty;

        public Guid RoleId { get; init; }

        public string RoleNameEn { get; init; } = string.Empty;

        public string RoleNameAr { get; init; } = string.Empty;
    }

    public sealed record SupervisorCollectionsPeriodResponse
    {
        public DateTime From { get; init; }

        public DateTime To { get; init; }
    }

    public sealed record SupervisorCollectionsSummaryResponse
    {
        public int TicketCollectionsCount { get; init; }

        public int ActiveTicketCollectionsCount { get; init; }

        public int ArchivedTicketCollectionsCount { get; init; }

        public decimal ActiveTicketCollectionsAmount { get; init; }

        public decimal ArchivedTicketCollectionsAmount { get; init; }

        public decimal TicketCollectionsAmount { get; init; }

        public int SupervisorExceedCollectionsCount { get; init; }

        public decimal SupervisorExceedCollectionsAmount { get; init; }

        public int PendingExceedCollectionsCount { get; init; }

        public decimal PendingExceedCollectionsAmount { get; init; }

        public decimal GrandTotalCollectionsAmount { get; init; }
    }

    public sealed record TicketCollectorSummaryResponse
    {
        public Guid CollectorId { get; init; }

        public string CollectorName { get; init; } = string.Empty;

        public int ActiveTicketsCount { get; init; }

        public int ArchivedTicketsCount { get; init; }

        public int TotalTicketsCount { get; init; }

        public decimal ActiveTicketsAmount { get; init; }

        public decimal ArchivedTicketsAmount { get; init; }

        public decimal TotalAmount { get; init; }
    }

    public sealed record ActiveTicketCollectionResponse
    {
        public Guid TicketId { get; init; }

        public string UniqueCode { get; init; } = string.Empty;

        public DateTime StartDate { get; init; }

        public DateTime? CollectedDate { get; init; }

        public Guid? CollectedBy { get; init; }

        public string? CollectorName { get; init; }

        public decimal CollectionAmount { get; init; }

        public string? VehicleNumber { get; init; }

        public bool IsSubscriber { get; init; }

        public string? SubscriberNationalId { get; init; }

        public string? CardNumber { get; init; }

        public Guid GateId { get; init; }

        public int GateNumber { get; init; }
    }

    public sealed record ArchivedTicketCollectionResponse
    {
        public Guid TicketId { get; init; }

        public string UniqueCode { get; init; } = string.Empty;

        public DateTime StartDate { get; init; }

        public DateTime EndDate { get; init; }

        public DateTime CollectedDate { get; init; }

        public Guid? CollectedBy { get; init; }

        public string? CollectorName { get; init; }

        public decimal CollectionAmount { get; init; }

        public string? VehicleNumber { get; init; }

        public bool IsSubscriber { get; init; }

        public string? SubscriberNationalId { get; init; }

        public string? CardNumber { get; init; }

        public Guid GateId { get; init; }

        public int GateNumber { get; init; }

        public Guid ExitGateId { get; init; }

        public int ExitGateNumber { get; init; }
    }

    public sealed record SupervisorExceedCollectionResponse
    {
        public Guid TransactionId { get; init; }

        public Guid TicketIdSnapshot { get; init; }

        public string TicketUniqueCode { get; init; } = string.Empty;

        public DateTime TicketStartDate { get; init; }

        public DateTime? TicketEndDate { get; init; }

        public decimal ExceedAmount { get; init; }

        public decimal TicketPrice { get; init; }

        public decimal TicketOldExceedPrice { get; init; }

        public decimal TotalPriceAfterExceed { get; init; }

        public string? VehicleNumber { get; init; }

        public string? CardNumber { get; init; }

        public bool IsSubscriber { get; init; }

        public string? SubscriberNationalId { get; init; }

        public Guid ExceedCreatedById { get; init; }

        public string ExceedCreatedByName { get; init; } = string.Empty;

        public Guid SupervisorCollectorId { get; init; }

        public string SupervisorCollectorName { get; init; } = string.Empty;

        public DateTime? SupervisorCollectedAt { get; init; }

        public DateTime CreatedAt { get; init; }

        public Guid GateId { get; init; }

        public int GateNumber { get; init; }

        public Guid TicketGateId { get; init; }

        public Guid? TicketExitGateId { get; init; }
    }

    public sealed record PendingExceedCollectionResponse
    {
        public Guid TransactionId { get; init; }

        public Guid TicketIdSnapshot { get; init; }

        public string TicketUniqueCode { get; init; } = string.Empty;

        public DateTime TicketStartDate { get; init; }

        public DateTime? TicketEndDate { get; init; }

        public decimal ExceedAmount { get; init; }

        public string? VehicleNumber { get; init; }

        public Guid ExceedCreatedById { get; init; }

        public string ExceedCreatedByName { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }

        public Guid GateId { get; init; }

        public int GateNumber { get; init; }
    }
}