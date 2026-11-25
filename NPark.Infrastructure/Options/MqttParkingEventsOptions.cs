namespace NPark.Infrastructure.Options;

public sealed class MqttParkingEventsOptions
{
    public bool Enabled { get; set; }
    public int SubscriptionQos { get; set; } = 1;
}
