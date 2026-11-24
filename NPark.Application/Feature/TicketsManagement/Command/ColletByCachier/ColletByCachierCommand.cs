using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Command.ColletByCachier
{
    public sealed record ColletByCachierCommand : ITransactionalCommand
    {
        public decimal? ExpectedBasePrice { get; init; }
        public Guid TicketId { get; init; }
    }
}