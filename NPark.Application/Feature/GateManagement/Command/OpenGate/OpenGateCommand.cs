using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.GateManagement.Command.OpenGate
{
    public sealed record OpenGateCommand : ICommand<OpenGateCommandResponse>
    {
        public string ControllerId { get; init; } = string.Empty;
    }

    public sealed record OpenGateCommandResponse
    {
        public string ControllerId { get; init; } = string.Empty;
        public string Topic { get; init; } = string.Empty;
        public string Payload { get; init; } = string.Empty;
        public DateTime PublishedAtUtc { get; init; }
    }
}