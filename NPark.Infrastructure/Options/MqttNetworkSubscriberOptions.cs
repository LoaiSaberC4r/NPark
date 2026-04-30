namespace NPark.Infrastructure.Options
{
    public sealed class MqttNetworkSubscriberOptions
    {
        public const string SectionName = "MqttNetworkSubscriber";

        public bool Enabled { get; set; } = true;

        public string TopicFilter { get; set; } = "publish/network/#";

        public int Qos { get; set; } = 1;

        public int ReconnectSeconds { get; set; } = 3;
    }
}