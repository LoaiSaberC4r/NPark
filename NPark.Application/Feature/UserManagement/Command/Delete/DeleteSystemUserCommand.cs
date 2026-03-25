using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.UserManagement.Command.Delete
{
    public sealed record DeleteSystemUserCommand : ICommand
    {
        public Guid UserId { get; init; }
    }
}