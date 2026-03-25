using BuildingBlock.Application.Abstraction;
using NPark.Application.Feature.TicketsManagement.Command.CalculateSalary;

namespace NPark.Application.Feature.TicketsManagement.Command.AddSantctionCommand
{
    public sealed record AddSanctionCommand : ICommand<CalculateSalaryCommandResponse>
    {
        public Guid TicketId { get; init; }

        public IReadOnlyCollection<Guid>? SanctionIds { get; init; }
    }
}