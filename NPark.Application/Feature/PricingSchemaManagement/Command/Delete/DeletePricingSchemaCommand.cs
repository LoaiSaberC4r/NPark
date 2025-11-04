using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Delete
{
    public sealed record DeletePricingSchemaCommand : ICommand
    {
        public Guid Id { get; init; }
    }
}