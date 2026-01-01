using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.AuthManagement.Command.ChangePassword
{
    public sealed record ChangePasswordCommand : ICommand
    {
        public string OldPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}