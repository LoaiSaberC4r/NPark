namespace NPark.Application.Abstraction
{
    public interface IMqttExitQrPacketDecoder
    {
        MqttExitQrPacketDecodeResult Decode(byte[] payloadBytes);
    }

    public sealed record MqttExitQrPacketDecodeResult
    {
        public bool IsSuccess { get; init; }

        public string? ErrorCode { get; init; }

        public string? ErrorMessage { get; init; }

        public string RawFrameHex { get; init; } = string.Empty;

        public bool HasControllerId { get; init; }

        public int? ControllerId { get; init; }

        public string ControllerIdHex { get; init; } = string.Empty;

        public string MyDataHex { get; init; } = string.Empty;

        public string ActualQrHex { get; init; } = string.Empty;

        public string QrString { get; init; } = string.Empty;
    }
}