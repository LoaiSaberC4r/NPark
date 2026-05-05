using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetEnterPriceSchemasForSelection
{
    public sealed record GetEnterPriceSchemasForSelectionQuery
        : IQuery<IReadOnlyList<GetEnterPriceSchemasForSelectionResponse>>;

    public sealed record GetEnterPriceSchemasForSelectionResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Salary { get; init; }
    }
}