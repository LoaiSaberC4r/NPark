using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Command.Add;

internal sealed record PersistIssuedTicket(Ticket Ticket) : ITransactionalCommand;

internal sealed class PersistIssuedTicketHandler(IUnitOfWork db) : ICommandHandler<PersistIssuedTicket>
{
    public async Task<Result> Handle(PersistIssuedTicket request, CancellationToken ct)
    {
        var ticket = request.Ticket;
        if (ticket.IsSubscriber && await db.AnyWithSpecAsync(
            new GetActiveSubscriberTicketSpec(ticket.CardNumber, ticket.SubscriberNationalId, ticket.VehicleNumber), ct))
            return Result.Fail(new Error("Subscriber.AlreadyInsidePark",
                NPark.Domain.Resource.ErrorMessage.Subscriber_AlreadyInsidePark, ErrorType.Conflict));

        await db.Repository<Ticket>().AddAsync(ticket, ct);
        return Result.Ok();
    }
}
