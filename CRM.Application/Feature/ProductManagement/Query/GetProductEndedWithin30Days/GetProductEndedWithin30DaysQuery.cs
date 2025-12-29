using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.SharedDto;

namespace CRM.Application.Feature.ProductManagement.Query.GetProductEndedWithin30Days
{
    public sealed record GetProductEndedWithin30DaysQuery : IQuery<Pagination<GetProductEndedWithin30DaysQueryResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}