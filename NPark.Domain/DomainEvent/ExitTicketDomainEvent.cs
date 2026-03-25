using BuildingBlock.Domain.Primitive;
using NPark.Domain.Entities;

namespace NPark.Domain.DomainEvent
{
    public sealed record class ExitTicketDomainEvent(Ticket Ticket) : IDomainEvent;
}