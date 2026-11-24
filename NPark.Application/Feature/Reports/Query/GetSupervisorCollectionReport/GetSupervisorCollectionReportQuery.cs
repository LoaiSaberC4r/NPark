using BuildingBlock.Application.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionReport
{
    public sealed record GetSupervisorCollectionReportQuery
         : IQuery<SupervisorCollectionReportResponse>
    {
        public Guid SupervisorId { get; init; }

        public DateTime FromDate { get; init; }

        public DateTime ToDate { get; init; }
    }

    public sealed record SupervisorCollectionReportResponse
    {
        public Guid SupervisorId { get; init; }

        public string SupervisorName { get; init; } = string.Empty;

        public DateTime FromDate { get; init; }

        public DateTime ToDate { get; init; }

        public int TotalCollectionTransactions { get; init; }

        public int TotalItemsCollected { get; init; }

        public decimal TotalAmountCollected { get; init; }

        public int TotalEnterTicketsCollected { get; init; }

        public decimal TotalEnterTicketsAmount { get; init; }

        public int TotalExceedTransactionsCollected { get; init; }

        public decimal TotalExceedAmount { get; init; }

        public List<SupervisorCollectionTransactionReportItem> Transactions { get; init; } = new();
    }

    public sealed record SupervisorCollectionTransactionReportItem
    {
        public Guid TransactionId { get; init; }

        public int TransactionTypeId { get; init; }

        /// <summary>
        /// EnterTickets | ExceedTransactions
        /// </summary>
        public string TransactionType { get; init; } = string.Empty;

        public Guid GateId { get; init; }

        public string GateName { get; init; } = string.Empty;

        public Guid CollectedBy { get; init; }

        public string CollectedByName { get; init; } = string.Empty;

        public DateTime CollectedAt { get; init; }

        public int TotalItemsCollected { get; init; }

        public decimal TotalAmountCollected { get; init; }

        public List<SupervisorCollectionLineReportItem> Lines { get; init; } = new();

        public List<SupervisorCollectionTicketReportItem> Tickets { get; init; } = new();
    }

    public sealed record SupervisorCollectionLineReportItem
    {
        public Guid ResponsibleUserId { get; init; }

        public string ResponsibleUserName { get; init; } = string.Empty;

        public int ItemsCount { get; init; }

        public decimal TotalAmount { get; init; }
    }

    public sealed record SupervisorCollectionTicketReportItem
    {
        public Guid ItemId { get; init; }

        public Guid ResponsibleUserId { get; init; }

        public string ResponsibleUserName { get; init; } = string.Empty;

        /// <summary>
        /// Ticket | TicketArchive | TicketExceedTransaction
        /// </summary>
        public string Source { get; init; } = string.Empty;

        public Guid TicketIdSnapshot { get; init; }

        public string TicketUniqueCode { get; init; } = string.Empty;

        public DateTime TicketStartDate { get; init; }

        public DateTime? TicketEndDate { get; init; }

        public decimal OriginalPrice { get; init; }

        public decimal ExceedAmount { get; init; }

        public decimal AmountCollected { get; init; }

        public string? VehicleNumber { get; init; }

        public string? CardNumber { get; init; }

        public bool IsSubscriber { get; init; }

        public string? SubscriberNationalId { get; init; }

        public Guid TicketGateId { get; init; }

        public Guid? TicketExitGateId { get; init; }
    }
}