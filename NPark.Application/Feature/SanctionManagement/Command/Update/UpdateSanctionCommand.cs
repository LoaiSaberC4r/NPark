using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.SanctionManagement.Command.Update
{
    public sealed record UpdateSanctionCommand : ICommand
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal Amount { get; init; }
    }
}