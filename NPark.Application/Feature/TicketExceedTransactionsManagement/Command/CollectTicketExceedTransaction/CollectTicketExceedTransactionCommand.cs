using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction
{
    public sealed record CollectTicketExceedTransactionCommand
        : ICommand<CollectTicketExceedTransactionResponse>
    {
    }
    public sealed record CollectTicketExceedTransactionResponse
    {
        public decimal TotalExceedAmountCollected { get; init; }

        public int TotalTransactionsCollected { get; init; }

        public List<TicketExceedCollectDetails> TicketExceedCollectDetails { get; init; } = new();
    }

    public sealed record TicketExceedCollectDetails
    {
        public Guid TransactionId { get; init; }

        public Guid TicketIdSnapshot { get; init; }

        public string TicketUniqueCode { get; init; } = string.Empty;

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