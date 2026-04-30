using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.Create
{
    public sealed record CreateTicketExceedTransactionCommand
       : ICommand<CreateTicketExceedTransactionCommandResponse>
    {
        public Guid TicketId { get; init; }

        public IReadOnlyCollection<Guid> SanctionIds { get; init; } = Array.Empty<Guid>();
    }

    public sealed record CreateTicketExceedTransactionCommandResponse
    {
        public Guid TicketExceedTransactionId { get; init; }
        public Guid TicketId { get; init; }

        public decimal ExceedAmount { get; init; }
        public decimal TicketPrice { get; init; }
        public decimal TotalPriceAfterExceed { get; init; }

        public Guid GateId { get; init; }
        public Guid UserCollectId { get; init; }
        public Guid? SuperVisorCollectorId { get; init; }

        public IReadOnlyCollection<Guid> SanctionIds { get; init; } = Array.Empty<Guid>();

        public DateTime CreatedAt { get; init; }
    }
}