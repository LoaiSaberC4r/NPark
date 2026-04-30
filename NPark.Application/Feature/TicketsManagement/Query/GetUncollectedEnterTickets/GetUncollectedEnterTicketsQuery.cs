using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Query.GetUncollectedEnterTickets
{
    public sealed record GetUncollectedEnterTicketsQuery
      : IQuery<GetUncollectedEnterTicketsResponse>
    {
    }
    public sealed record GetUncollectedEnterTicketsResponse
    {
        public Guid GateId { get; init; }

        public int TotalTicketsCount { get; init; }

        public decimal TotalOriginalPrice { get; init; }

        public List<UncollectedEnterTicketDetails> Tickets { get; init; } = new();
    }

    public sealed record UncollectedEnterTicketDetails
    {
        public Guid TicketId { get; init; }

        public string TicketUniqueCode { get; init; } = string.Empty;

        /// <summary>
        /// Ticket | TicketArchive
        /// </summary>
        public string Source { get; init; } = string.Empty;

        public DateTime StartDate { get; init; }

        public DateTime? EndDate { get; init; }

        public decimal OriginalPrice { get; init; }

        public string? VehicleNumber { get; init; }

        public string? CardNumber { get; init; }

        public bool IsSubscriber { get; init; }

        public Guid GateId { get; init; }

        public Guid? ExitGateId { get; init; }
    }
}