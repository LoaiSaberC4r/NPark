using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetOrderSchema
{
    public sealed record GetOrderSchemaQuery : IQuery<IReadOnlyList<GetOrderSchemaQueryResponse>>
    {
    }
}