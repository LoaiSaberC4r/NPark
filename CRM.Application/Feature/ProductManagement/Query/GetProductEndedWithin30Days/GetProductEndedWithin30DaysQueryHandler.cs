using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.SharedDto;
using CRM.Application.Specification.ProductSpecification;
using CRM.Domain.Entities;

namespace CRM.Application.Feature.ProductManagement.Query.GetProductEndedWithin30Days
{
    internal sealed class GetProductEndedWithin30DaysQueryHandler : IQueryHandler<GetProductEndedWithin30DaysQuery, Pagination<GetProductEndedWithin30DaysQueryResponse>>
    {
        private readonly IGenericRepository<SiteProduct> _repository;

        public GetProductEndedWithin30DaysQueryHandler(IGenericRepository<SiteProduct> repository) => _repository = repository;

        public async Task<Result<Pagination<GetProductEndedWithin30DaysQueryResponse>>> Handle(GetProductEndedWithin30DaysQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetProductEndedWithin30DaysQuerySpec(request.PageNumber, request.PageSize);
            var result = _repository.GetWithSpec(spec);
            var response = new Pagination<GetProductEndedWithin30DaysQueryResponse>(request.PageNumber, request.PageSize, result.count, result.data.ToList()); ;

            return Result<Pagination<GetProductEndedWithin30DaysQueryResponse>>.Ok(response);
        }
    }
}