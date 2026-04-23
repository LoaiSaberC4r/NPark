namespace NPark.Infrastructure.Options
{
    public sealed class CameraOptions
    {
        public bool Enabled { get; set; } = true;

        public string DeviceServiceUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 15;

        // Optional fallback if camera already exposes direct JPEG endpoint
        public string? SnapshotUrlOverride { get; set; }
    }
}