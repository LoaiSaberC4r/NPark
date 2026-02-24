using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.SanctionManagement.Command.Delete
{
    public sealed record DeleteSanctionCommand : ICommand
    {
        public Guid Id { get; init; }
    }
}