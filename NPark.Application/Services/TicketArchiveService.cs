using BuildingBlock.Application.Repositories;
using NPark.Application.Feature.TicketsManagement.Command.ExitTicket;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Application.Services;

public interface ITicketArchiveService
{
    Task<TicketExitedNotification> CompleteAsync(GateOperation operation, CancellationToken ct);
}

public sealed class TicketArchiveService(IUnitOfWork db, ParkingBusinessClock clock) : ITicketArchiveService
{
    public async Task<TicketExitedNotification> CompleteAsync(GateOperation operation, CancellationToken ct)
    {
        if (operation.Type != GateOperationType.Exit || operation.TicketIdSnapshot is not Guid ticketId || ticketId == Guid.Empty)
            throw new InvalidOperationException("Archiving requires an exit operation with a valid ticket.");

        var confirmedAt = operation.CompletionMode switch
        {
            GateOperationCompletionMode.Hardware when operation.Status == GateOperationStatus.Acknowledged &&
                operation.AckStatusCode == 0 && operation.AcknowledgedAtUtc is DateTime acknowledgedAt => acknowledgedAt,
            GateOperationCompletionMode.Software when operation.Status == GateOperationStatus.Completed &&
                operation.CompletedAtUtc is DateTime completedAt && operation.DispatchStartedAtUtc is null &&
                operation.PublishedAtUtc is null && operation.AcknowledgedAtUtc is null && operation.AckStatusCode is null => completedAt,
            _ => throw new InvalidOperationException("Archiving requires valid exit confirmation.")
        };

        var archive = await db.Repository<TicketArchive>().GetByIdAsync(ticketId, ct);
        var ticket = await db.FirstOrDefaultWithSpecAsync(new GetTicketForExitArchiveSpec(ticketId), ct);
        if (archive != null)
        {
            if (ticket != null || archive.ExitOperationId != operation.Id)
                throw new InvalidOperationException($"Inconsistent active or historical records for ticket {ticketId}.");
        }
        else
        {
            if (ticket is null)
                throw new InvalidOperationException($"Active ticket {ticketId} was not found.");

            ticket.CompleteExit(operation.GateId, clock.ToBusinessTime(confirmedAt));
            archive = TicketArchive.CreateFromTicket(ticket, operation.Id);
            await db.Repository<TicketArchive>().AddAsync(archive, ct);
            db.Repository<Ticket>().Delete(ticket);
        }

        var gate = await db.Repository<ParkingGate>().GetByIdAsync(operation.GateId, ct);
        return new()
        {
            TicketId = archive.Id,
            GateId = operation.GateId,
            UserId = operation.UserId ?? archive.UserId,
            ExitDate = archive.EndDate,
            GateNumber = gate?.GateNumber.ToString() ?? string.Empty
        };
    }
}
