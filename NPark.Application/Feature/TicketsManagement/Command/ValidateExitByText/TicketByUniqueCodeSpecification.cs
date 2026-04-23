using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText
{
    public sealed class TicketByUniqueCodeSpecification : Specification<Ticket>
    {
        public TicketByUniqueCodeSpecification(string uniqueCode)
        {
            AddCriteria(t => t.UniqueCode == uniqueCode);
        }
    }
}