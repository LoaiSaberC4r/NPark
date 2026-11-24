using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Command.CalculateSalary
{
    public sealed record CalculateSalaryCommand : ITransactionalCommand<CalculateSalaryCommandResponse>
    {
        public Guid TicketId { get; init; }
        public IReadOnlyCollection<Guid>? SanctionIds { get; init; }
    }
}