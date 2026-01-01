using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.SupportManagement.Command.CloseTicket
{
    public sealed record CloseTicketTicketCommand : ICommand
    {
        public string? Description { get; init; }
        public Guid TicketId { get; init; }
    }
}