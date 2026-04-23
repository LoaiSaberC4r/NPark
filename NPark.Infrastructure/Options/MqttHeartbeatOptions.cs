namespace NPark.Infrastructure.Options
{
    public sealed class MqttHeartbeatOptions
    {
        public bool Enabled { get; set; } = true;

        public int IntervalSeconds { get; set; } = 20;

        public string TopicPrefix { get; set; } = "sub/network";

        public string DeviceId { get; set; } = "FF";

        /// <summary>
        /// Hex payload as plain text, exactly as required by device/protocol.
        /// Example: "7B FF A3 1A 2A 3A 4A 7D"
        /// </summary>
        public string Payload { get; set; } = "7B FF A3 1A 2A 3A 4A 7D";

        public int Qos { get; set; } = 1;

        public bool Retain { get; set; } = false;
    }
}