using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Specifications.PricingSchemaSpecification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetOrderSchema
{
    public sealed class GetOrderSchemaQueryHandler : IQueryHandler<GetOrderSchemaQuery, IReadOnlyList<GetOrderSchemaQueryResponse>>
    {
        private readonly IGenericRepository<OrderPricingSchema> _repository;

        public GetOrderSchemaQueryHandler(IGenericRepository<OrderPricingSchema> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<IReadOnlyList<GetOrderSchemaQueryResponse>>> Handle(GetOrderSchemaQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetOrderSchemaSpecificaiton();
            var entites = await _repository.ListWithSpecAsync(spec, cancellationToken);
            return Result<IReadOnlyList<GetOrderSchemaQueryResponse>>.Ok(entites);
        }
    }
}