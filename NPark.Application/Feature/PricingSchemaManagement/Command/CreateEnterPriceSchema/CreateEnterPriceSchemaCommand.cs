using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.CreateEnterPriceSchema
{
    public sealed record CreateEnterPriceSchemaCommand
       : ICommand<CreateEnterPriceSchemaCommandResponse>
    {
        public string Name { get; init; } = string.Empty;
        public decimal Salary { get; init; }
    }

    public sealed record CreateEnterPriceSchemaCommandResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Salary { get; init; }
        public bool IsEnterPriceSchema { get; init; }
    }
}