using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.Create
{
    public sealed record CreateTicketExceedTransactionCommand
       : ITransactionalCommand<CreateTicketExceedTransactionCommandResponse>
    {
        public Guid TicketId { get; init; }

        public string? IdempotencyKey { get; init; }
        public IReadOnlyCollection<Guid> SanctionIds { get; init; } = Array.Empty<Guid>();
    }

    public sealed record CreateTicketExceedTransactionCommandResponse
    {
        public Guid TicketExceedTransactionId { get; init; }
        public Guid TicketId { get; init; }

        public decimal ExceedAmount { get; init; }
        public decimal ExceedPrice { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal OutstandingAmount { get; init; }
        public DateOnly ValidExitDate { get; init; }
        public decimal TicketPrice { get; init; }
        public decimal TotalPriceAfterExceed { get; init; }

        public Guid GateId { get; init; }
        public Guid UserCollectId { get; init; }
        public Guid? SuperVisorCollectorId { get; init; }

        public IReadOnlyCollection<Guid> SanctionIds { get; init; } = Array.Empty<Guid>();

        public string PaymentStatus { get; init; } = string.Empty;
        public DateTime? PaymentConfirmedAtUtc { get; init; }
        public DateTime CreatedAt { get; init; }

        public byte[] QrCode { get; init; } = Array.Empty<byte>();

        public string? TicketInfo { get; init; } = string.Empty;
    }
}