using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.SupportManagement.Command.Add
{
    public sealed record AddSupportCommand : ICommand
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
    }
}