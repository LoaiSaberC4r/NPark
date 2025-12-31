using CRM.Application.Shared.Dto;

namespace CRM.Application.Feature.AuthManagement.Command.Login
{
    public sealed record LoginResponseCommand
    {
        public UserTokenDto TokenInfo { get; init; } = new UserTokenDto();
        public bool IsFirstLogin { get; init; } = false;
    }
}