using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Command.ExitTicketFromMqtt.ExitTicketFromMqttCommand
{
    public sealed record ExitTicketFromMqttCommand
       : ICommand<ExitTicketFromMqttCommandResponse>
    {
        public Guid TicketId { get; init; }

        public string ControllerId { get; init; } = string.Empty;

        public Guid? UserId { get; init; }
    }

    public sealed record ExitTicketFromMqttCommandResponse
    {
        public Guid TicketId { get; init; }

        public Guid GateId { get; init; }

        public string ControllerId { get; init; } = string.Empty;

        public Guid? UserId { get; init; }

        public DateTime ExitDate { get; init; }

        public bool TicketExited { get; init; }

        public bool Archived { get; init; }
    }
}