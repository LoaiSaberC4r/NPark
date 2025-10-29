using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.SharedDto;
using NPark.Application.Specifications.PricingSchemaSpecification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetAll
{
    public sealed class GetAllPricingSchemaQueryHandler : IQueryHandler<GetAllPricingSchemaQuery, Pagination<GetAllPricingSchemaQueryResponse>>
    {
        private readonly IGenericRepository<PricingScheme> _repo;

        public GetAllPricingSchemaQueryHandler(IGenericRepository<PricingScheme> repo)
        {
            _repo = repo;
        }

        public async Task<Result<Pagination<GetAllPricingSchemaQueryResponse>>> Handle(GetAllPricingSchemaQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetAllPricingSchemaSpecification(request);
            var result = _repo.GetWithSpec(spec);
            var response = new Pagination<GetAllPricingSchemaQueryResponse>
                (request.PageNumber, request.PageSize, result.count, result.data.ToList());
            return Result<Pagination<GetAllPricingSchemaQueryResponse>>.Ok(response);
        }
    }
}