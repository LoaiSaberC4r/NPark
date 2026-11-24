using BuildingBlock.Application.Repositories;
using NPark.Application.Feature.TicketsManagement.Command.ExitTicket;
using NPark.Domain.Entities;

namespace NPark.Application.Services;

public interface ITicketArchiveService
{
    Task<TicketArchive> CompleteAsync(GateOperation operation, CancellationToken ct);
}

public sealed class TicketArchiveService(IUnitOfWork db, ParkingBusinessClock clock) : ITicketArchiveService
{
    public async Task<TicketArchive> CompleteAsync(GateOperation operation, CancellationToken ct)
    {
        if (operation.AckStatusCode != 0 || operation.AcknowledgedAtUtc is not DateTime acknowledgedAt ||
            operation.TicketIdSnapshot is not Guid ticketId)
            throw new InvalidOperationException("The exit operation has no confirmed acknowledgment.");

        var existing = await db.Repository<TicketArchive>().GetByIdAsync(ticketId, ct);
        var ticket = await db.FirstOrDefaultWithSpecAsync(new GetTicketForExitArchiveSpec(ticketId), ct);
        if (existing != null)
        {
            if (ticket != null || existing.ExitOperationId != operation.Id)
                throw new InvalidOperationException($"Inconsistent active or historical records for ticket {ticketId}.");
            return existing;
        }
        if (ticket is null)
            throw new InvalidOperationException($"Active ticket {ticketId} was not found.");

        ticket.CompleteExit(operation.GateId, clock.ToBusinessTime(acknowledgedAt));
        var archive = TicketArchive.CreateFromTicket(ticket, operation.Id);
        await db.Repository<TicketArchive>().AddAsync(archive, ct);
        db.Repository<Ticket>().Delete(ticket);
        return archive;
    }
}
