using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetPricingSchema
{
    public sealed record GetPricingSchemaQuery : IQuery<IReadOnlyList<GetPricingSchemaQueryResponse>>
    {
    }
}