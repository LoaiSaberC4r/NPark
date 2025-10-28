using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.Auth.Command.LoginFirstTime
{
    public sealed record LoginFirstTimeCommand : ICommand
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmedPassword { get; set; } = string.Empty;
    }
}