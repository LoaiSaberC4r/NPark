namespace NPark.Application.Abstraction
{
    public interface IMqttPublisher
    {
        Task PublishStringAsync(string topic, string payload, MqttPublishOptions options, CancellationToken ct);

        Task PublishBytesAsync(string topic, ReadOnlyMemory<byte> payload, MqttPublishOptions options, CancellationToken ct);
    }

    public sealed record MqttPublishOptions(int Qos = 1, bool Retain = false);
}