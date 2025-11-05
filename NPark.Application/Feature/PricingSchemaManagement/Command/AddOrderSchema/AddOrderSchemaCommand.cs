using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.AddOrderSchema
{
    public sealed record AddOrderSchemaCommand : ICommand
    {
        public List<AddOrderSchemaDto> OrderSchema { get; init; } = new List<AddOrderSchemaDto>();
    }

    public record AddOrderSchemaDto
    {
        public Guid PricingSchemaId { get; init; }
        public int Count { get; init; }
    }
}