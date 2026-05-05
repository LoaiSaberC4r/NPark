using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.UserManagement.Command.Update
{
    public sealed record UpdateUserCommand : ICommand
    {
        public Guid Id { get; init; }

        public string Email { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public Guid RoleId { get; init; }

        public string? Password { get; init; }

        public TimeSpan? ShiftStartTime { get; init; }
        public TimeSpan? ShiftEndTime { get; init; }
    }
}