using BuildingBlock.Application.Abstraction;
using NPark.Application.Services;

namespace NPark.Application.Feature.GateManagement.Command.OpenGate;

public sealed record OpenGateCommand : ICommand<GateOperationResponse>
{
    public string ControllerId { get; init; } = string.Empty;
    public Guid? TicketId { get; init; }
    public string? OperationType { get; init; }
}
