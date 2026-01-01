using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.AuthManagement.Command.Login
{
    public sealed record LoginCommand : ICommand<LoginResponseCommand>
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }
}