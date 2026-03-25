using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Command.CalculateSalary
{
    public sealed record CalculateSalaryCommand : ICommand<CalculateSalaryCommandResponse>
    {
        public Guid TicketId { get; init; }
        public IReadOnlyCollection<Guid>? SanctionIds { get; init; }
    }
}