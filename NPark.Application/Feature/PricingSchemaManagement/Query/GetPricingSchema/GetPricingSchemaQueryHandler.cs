using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Specifications.PricingSchemaSpecification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetPricingSchema
{
    public sealed class GetPricingSchemaQueryHandler : IQueryHandler<GetPricingSchemaQuery, IReadOnlyList<GetPricingSchemaQueryResponse>>
    {
        private readonly IGenericRepository<PricingScheme> _repository;

        public GetPricingSchemaQueryHandler(IGenericRepository<PricingScheme> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<IReadOnlyList<GetPricingSchemaQueryResponse>>> Handle(GetPricingSchemaQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetPricingSchemaQuerySpecification();
            var result = await _repository.ListWithSpecAsync(spec);
            return Result<IReadOnlyList<GetPricingSchemaQueryResponse>>.Ok(result);
        }
    }
}