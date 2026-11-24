using NPark.Application.Abstraction.Security;
using Microsoft.AspNetCore.SignalR;
using NPark.Application.Abstraction;

namespace NPark.Api.RealTime
{
    public sealed class NotificationsHub(ITokenReader tokens) : Hub<IRealtimeNotificationClient>
    {
        internal static string GateGroup(Guid gateId) => $"gate:{gateId:D}";

        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated == true && Context.User.GetGateId(tokens) is Guid gateId)
                await Groups.AddToGroupAsync(Context.ConnectionId, GateGroup(gateId), Context.ConnectionAborted);
            await base.OnConnectedAsync();
        }
    }
}