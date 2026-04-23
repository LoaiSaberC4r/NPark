namespace NPark.Infrastructure.Options
{
    public sealed class MqttOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int KeepAliveSeconds { get; set; } = 20;
        public int ReconnectSeconds { get; set; } = 3;
        public bool UseTls { get; set; } = true;
    }
}