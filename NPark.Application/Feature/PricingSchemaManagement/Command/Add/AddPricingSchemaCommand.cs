using BuildingBlock.Application.Abstraction;
using NPark.Domain.Enums;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Add
{
    public sealed record AddPricingSchemaCommand : ICommand
    {
        public string Name { get; set; }
        public DurationType DurationType { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public decimal Price { get; set; }
        public bool IsRepeated { get; set; }
        public int? TotalDays { get; set; }
        public int? TotalHours { get; set; }
    }
}