using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.SharedDto;
using CRM.Application.Specification.ProductSpecification;
using CRM.Domain.Entities;

namespace CRM.Application.Feature.ProductManagement.Query.GetClosedProducts
{
    internal sealed class GetClosedProductsHandler : IQueryHandler<GetClosedProductsQuery, Pagination<GetClosedProductsResponse>>
    {
        private readonly IGenericRepository<SiteProduct> _repository;

        public GetClosedProductsHandler(IGenericRepository<SiteProduct> repository)
        {
            _repository = repository;
        }

        public async Task<Result<Pagination<GetClosedProductsResponse>>> Handle(GetClosedProductsQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetClosedProductsQuerySpec(request.PageNumber, request.PageSize);
            var result = _repository.GetWithSpec(spec);
            var response = new Pagination<GetClosedProductsResponse>(request.PageNumber, request.PageSize, result.count, result.data.ToList()); ;

            return Result<Pagination<GetClosedProductsResponse>>.Ok(response);
        }
    }
}