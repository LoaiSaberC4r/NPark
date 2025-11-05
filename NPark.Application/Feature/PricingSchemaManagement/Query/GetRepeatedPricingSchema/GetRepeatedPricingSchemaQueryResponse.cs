using NPark.Domain.Enums;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetRepeatedPricingSchema
{
    public sealed record GetRepeatedPricingSchemaQueryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsRepeated { get; set; }
        public int? TotalHours { get; set; }
        public DurationType DurationType { get; set; }
    }
}