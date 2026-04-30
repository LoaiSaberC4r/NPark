using NPark.Application.Feature.MqttNetworkManagement;

namespace NPark.Application.Abstraction
{
    public interface IMqttCaptureSignalRNotifier
    {
        Task NotifyCaptureDetectedAsync(
            MqttCaptureDetectedResponse payload,
            CancellationToken cancellationToken);
    }
}