using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Command.CollectEnterTickets
{
    public sealed record CollectEnterTicketsCommand : ICommand<CollectEnterTicketsResponse>
    {
    }

    public sealed record CollectEnterTicketsResponse
    {
        public Guid GateId { get; init; }

        public Guid CollectedBy { get; init; }

        public DateTime CollectedAt { get; init; }

        public int TotalTicketsCollected { get; init; }

        public decimal TotalOriginalPriceCollected { get; init; }

        public List<CollectedEnterTicketDetails> Tickets { get; init; } = new();
    }

    public sealed record CollectedEnterTicketDetails
    {
        public Guid TicketId { get; init; }

        public string TicketUniqueCode { get; init; } = string.Empty;

        /// <summary>
        /// Ticket | TicketArchive
        /// </summary>
        public string Source { get; init; } = string.Empty;

        public DateTime StartDate { get; init; }

        public DateTime? EndDate { get; init; }

        public DateTime CollectedAt { get; init; }

        /// <summary>
        /// Price only without ExceedPrice.
        /// </summary>
        public decimal OriginalPrice { get; init; }

        public string? VehicleNumber { get; init; }

        public string? CardNumber { get; init; }

        public bool IsSubscriber { get; init; }

        public Guid GateId { get; init; }

        public Guid? ExitGateId { get; init; }
    }
}