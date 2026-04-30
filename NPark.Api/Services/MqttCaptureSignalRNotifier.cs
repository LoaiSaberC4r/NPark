using Microsoft.AspNetCore.SignalR;
using NPark.Api.Hubs;
using NPark.Application.Abstraction;
using NPark.Application.Feature.MqttNetworkManagement;

namespace NPark.Api.Services
{
    internal sealed class MqttCaptureSignalRNotifier : IMqttCaptureSignalRNotifier
    {
        private readonly IHubContext<LprHub> _hubContext;

        public MqttCaptureSignalRNotifier(IHubContext<LprHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyCaptureDetectedAsync(
            MqttCaptureDetectedResponse payload,
            CancellationToken cancellationToken)
        {
            await _hubContext.Clients.All.SendAsync(
                "mqttCaptureDetected",
                payload,
                cancellationToken);
        }
    }
}