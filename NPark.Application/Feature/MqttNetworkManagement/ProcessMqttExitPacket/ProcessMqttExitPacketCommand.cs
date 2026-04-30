using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.MqttNetworkManagement.ProcessMqttExitPacket
{
    public sealed record ProcessMqttExitPacketCommand
        : ICommand<ProcessMqttExitPacketResponse>
    {
        public string DeviceId { get; init; } = string.Empty;

        public byte[] PayloadBytes { get; init; } = Array.Empty<byte>();
    }

    public sealed record ProcessMqttExitPacketResponse
    {
        public string DeviceId { get; init; } = string.Empty;

        public int ControllerIdFromPacket { get; init; }

        public string PayloadHex { get; init; } = string.Empty;

        public string MyDataHex { get; init; } = string.Empty;

        public string ActualQrHex { get; init; } = string.Empty;

        public string QrString { get; init; } = string.Empty;

        public Guid? TicketId { get; init; }

        public bool IsValidForExit { get; init; }

        public bool TicketExited { get; init; }

        public bool GateOpened { get; init; }

        public string? OpenGateTopic { get; init; }

        public string? OpenGatePayload { get; init; }

        public string? ErrorCode { get; init; }

        public string? ErrorMessage { get; init; }

        public bool WorkflowSucceeded => IsValidForExit && TicketExited && GateOpened;
    }
}