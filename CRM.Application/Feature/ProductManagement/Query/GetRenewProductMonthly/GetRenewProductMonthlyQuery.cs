using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.SharedDto;

namespace CRM.Application.Feature.ProductManagement.Query.GetRenewProductMonthly
{
    public sealed record GetRenewProductMonthlyQuery : IQuery<Pagination<GetRenewProductMonthlyQueryResponse>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }
}