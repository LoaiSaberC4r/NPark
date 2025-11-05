using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Specifications.PricingSchemaSpecification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetRepeatedPricingSchema
{
    public sealed class GetRepeatedPricingSchemaQueryHandler : IQueryHandler<GetRepeatedPricingSchemaQuery, IReadOnlyList<GetRepeatedPricingSchemaQueryResponse>>
    {
        private readonly IGenericRepository<PricingScheme> _repository;

        public GetRepeatedPricingSchemaQueryHandler(IGenericRepository<PricingScheme> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<IReadOnlyList<GetRepeatedPricingSchemaQueryResponse>>> Handle(GetRepeatedPricingSchemaQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetRepeatedPricingSchemaSpecification();
            var result = await _repository.ListWithSpecAsync(spec, cancellationToken);
            return Result<IReadOnlyList<GetRepeatedPricingSchemaQueryResponse>>.Ok(result);
        }
    }
}