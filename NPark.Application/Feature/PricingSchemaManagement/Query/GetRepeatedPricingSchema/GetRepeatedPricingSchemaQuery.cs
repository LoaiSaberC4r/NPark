using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetRepeatedPricingSchema
{
    public sealed record GetRepeatedPricingSchemaQuery : IQuery<IReadOnlyList<GetRepeatedPricingSchemaQueryResponse>>
    {
    }
}