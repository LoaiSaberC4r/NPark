using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.SupportManagement.Command.Reject
{
    public sealed record RejectSupporterCommand : ICommand
    {
        public string RejectReason { get; init; } = string.Empty;
        public Guid TicketId { get; init; }
    }
}