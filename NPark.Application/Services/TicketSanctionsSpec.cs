using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Services;

public sealed class TicketSanctionsSpec : Specification<TicketExceedTransaction>
{
    public TicketSanctionsSpec(Guid ticketId)
    {
        AddCriteria(x => x.TicketIdSnapshot == ticketId);
        Include(x => x.Sanctions);
    }
}
