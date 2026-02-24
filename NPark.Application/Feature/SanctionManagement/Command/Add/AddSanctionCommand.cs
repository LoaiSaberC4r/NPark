using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.SanctionManagement.Command.Add
{
    public sealed record AddSanctionCommand : ICommand
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal Amount { get; init; }
    }
}