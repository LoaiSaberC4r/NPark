using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.SharedDto;

namespace CRM.Application.Feature.ProductManagement.Query.GetClosedProducts
{
    public sealed record GetClosedProductsQuery : IQuery<Pagination<GetClosedProductsResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}