using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction
{
    public sealed record CollectTicketExceedTransactionCommand
        : ICommand<CollectTicketExceedTransactionResponse>
    {
    }

    public sealed record CollectTicketExceedTransactionResponse
    {
        public Guid CollectionTransactionId { get; init; }

        public Guid GateId { get; init; }

        public Guid CollectedBy { get; init; }
        public string CollectedName { get; init; } = string.Empty;

        public DateTime CollectedAt { get; init; }

        public decimal TotalExceedAmountCollected { get; init; }

        public int TotalTransactionsCollected { get; init; }

        public List<TicketExceedCollectLine> Lines { get; init; } = new();

        public List<TicketExceedCollectDetails> TicketExceedCollectDetails { get; init; } = new();
    }

    public sealed record TicketExceedCollectLine
    {
        /// <summary>
        /// User who created/collected the exceed transaction.
        /// </summary>
        public Guid ResponsibleUserId { get; init; }

        public string ResponsibleUserName { get; init; } = string.Empty;

        public int TransactionsCount { get; init; }

        public decimal TotalExceedAmount { get; init; }
    }

    public sealed record TicketExceedCollectDetails
    {
        public Guid TransactionId { get; init; }

        public Guid TicketIdSnapshot { get; init; }

        public string TicketUniqueCode { get; init; } = string.Empty;

        public Guid ResponsibleUserId { get; init; }

        public string ResponsibleUserName { get; init; } = string.Empty;

        public DateTime TransactionCreatedAt { get; init; }

        public DateTime SupervisorCollectedAt { get; init; }

        public decimal TicketPrice { get; init; }

        public decimal TicketOldExceedPrice { get; init; }

        public decimal ExceedAmount { get; init; }

        public decimal TotalPriceAfterExceed { get; init; }

        public string? VehicleNumber { get; init; }

        public string? CardNumber { get; init; }

        public bool IsSubscriber { get; init; }
    }
}