using NPark.Domain.Enums;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetOrderSchema
{
    public class GetOrderSchemaQueryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsRepeated { get; set; }
        public int? TotalHours { get; set; }
        public DurationType DurationType { get; set; }
        public int Count { get; set; }
    }
}