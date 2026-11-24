using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Command.CollectEnterTickets
{
    public sealed record CollectEnterTicketsCommand : ICommand<CollectEnterTicketsResponse>
    {
    }

    public sealed record CollectEnterTicketsResponse
    {
        public Guid TransactionId { get; init; }

        public Guid GateId { get; init; }

        public Guid CollectedBy { get; init; }
        public string CollectedName { get; init; } = string.Empty;

        public DateTime CollectedAt { get; init; }

        public int TotalTicketsCollected { get; init; }

        public decimal TotalOriginalPriceCollected { get; init; }

        /// <summary>
        /// Grouped summary by the user who created the ticket.
        /// </summary>
        public List<CollectedEnterTicketLine> Lines { get; init; } = new();

        public List<CollectedEnterTicketDetails> Tickets { get; init; } = new();
    }

    public sealed record CollectedEnterTicketLine
    {
        /// <summary>
        /// The user who created the tickets.
        /// </summary>
        public Guid ResponsibleUserId { get; init; }
        public string ResponsibleUserName { get; init; } = string.Empty;

        public int TicketsCount { get; init; }

        public decimal TotalOriginalPrice { get; init; }
    }

    public sealed record CollectedEnterTicketDetails
    {
        public Guid TicketId { get; init; }

        public string TicketUniqueCode { get; init; } = string.Empty;

        /// <summary>
        /// Ticket | TicketArchive
        /// </summary>
        public string Source { get; init; } = string.Empty;

        /// <summary>
        /// The user who created the ticket.
        /// </summary>
        public Guid ResponsibleUserId { get; init; }

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