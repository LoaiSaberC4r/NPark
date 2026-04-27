using Microsoft.AspNetCore.SignalR;
using NPark.Api.Hubs;
using NPark.Application.Abstraction;
using NPark.Application.Feature.LprSignalRManagement.Command.VehicleDetected;

namespace NPark.Api.Services
{
    internal sealed class LprSignalRNotifier : ILprSignalRNotifier
    {
        private readonly IHubContext<LprHub> _hubContext;

        public LprSignalRNotifier(IHubContext<LprHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyVehicleDetectedAsync(
            LprVehicleDetectedCommandResponse payload,
            CancellationToken cancellationToken)
        {
            await _hubContext.Clients.All.SendAsync(
                "lprVehicleDetected",
                payload,
                cancellationToken);
        }
    }
}