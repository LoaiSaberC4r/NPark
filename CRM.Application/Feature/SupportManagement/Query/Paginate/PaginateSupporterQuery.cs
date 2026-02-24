using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.SharedDto;

namespace CRM.Application.Feature.SupportManagement.Query.Paginate
{
    public sealed record PaginateSupporterQuery : IQuery<Pagination<PaginateSupporterQueryResponse>>
    {
        public string? SearchText { get; init; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}