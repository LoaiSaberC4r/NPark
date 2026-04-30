using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.SharedDto;

namespace NPark.Application.Feature.TicketsManagement.Query.SearchTicket
{
    public sealed class SearchTicketQuery
        : SearchParameters, IQuery<IReadOnlyList<SearchTicketQueryResponse>>
    {
        public DateTime? StartDate { get; set; }
    }

    public sealed record SearchTicketQueryResponse
    {
        public Guid Id { get; init; }
        public DateTime StartDate { get; init; }
        public string? VehicleNumber { get; init; }
        public string? CardNumber { get; init; }
        public string UniqueCode { get; init; } = string.Empty;
        public bool IsSubscribe { get; init; }
        public decimal Price { get; init; }
    }
}