using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Query.ReportTickets
{
    public sealed record class ReportTicketsQuery : IQuery<IReadOnlyCollection<ReportTicketsResponse>>
    {
        public DateTime FromDate { get; init; }

        public DateTime ToDate { get; init; }

        public string? SearchTerm { get; init; }
    }
}