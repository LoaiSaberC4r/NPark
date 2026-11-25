using BuildingBlock.Application.Abstraction;
using NPark.Application.Services;

namespace NPark.Application.Feature.TicketsManagement.Command.ExitTicket;

public sealed record ExitTicketCommand : ICommand<GateOperationResponse>
{
    public Guid TicketId { get; init; }
    public Guid? RequestId { get; init; }
}
