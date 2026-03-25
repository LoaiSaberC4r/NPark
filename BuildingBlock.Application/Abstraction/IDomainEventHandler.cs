using BuildingBlock.Domain.Primitive;
using MediatR;

namespace BuildingBlock.Application.Abstraction
{
    public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent>
        where TDomainEvent : IDomainEvent
    {
    }
}