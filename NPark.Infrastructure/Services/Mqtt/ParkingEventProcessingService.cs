using BuildingBlock.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPark.Application.Feature.TicketsManagement.Command.Add;
using NPark.Application.Options;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Infrastructure.Options;

namespace NPark.Infrastructure.Services.Mqtt;

internal sealed class ParkingEventProcessingService(IServiceScopeFactory scopes, ParkingEventStorage storage,
    IOptions<MqttParkingEventsOptions> mqtt, IOptions<AutomaticVisitorEntryOptions> entry,
    TimeProvider time, ILogger<ParkingEventProcessingService> logger) : BackgroundService
{
    private const int BatchSize = 32;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!mqtt.Value.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        do
        {
            try
            {
                await ProcessBatchAsync(ct);
            }
            catch (Exception error) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(error, "Parking event recovery pass failed");
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
        var now = time.GetUtcNow().UtcDateTime;
        var ackRetryCutoff = now.AddSeconds(-2);
        var events = db.ParkingHardwareEvents.AsNoTracking()
            .Where(x => x.RetryAfterUtc == null || x.RetryAfterUtc <= now);
        var unacknowledged = await events.Where(x => x.StorageAckSentAtUtc == null && x.ReceivedAtUtc <= ackRetryCutoff)
            .OrderBy(x => x.ReceivedAtUtc).ThenBy(x => x.Id).Take(BatchSize).ToListAsync(ct);

        var actionable = await events.Where(x => x.StorageAckSentAtUtc != null &&
                (entry.Value.Enabled && (x.ProcessingStatus == ParkingEventProcessingStatus.Received ||
                    x.ProcessingStatus == ParkingEventProcessingStatus.TicketCreated) ||
                x.ProcessingStatus == ParkingEventProcessingStatus.GateOperationCreated &&
                db.Set<GateOperation>().Any(op => op.Id == x.GateOperationId &&
                    (op.Status == GateOperationStatus.Completed || op.Status == GateOperationStatus.Failed ||
                        op.Status == GateOperationStatus.TimedOut || op.Status == GateOperationStatus.Unknown))))
            .OrderBy(x => x.ReceivedAtUtc).ThenBy(x => x.Id).Take(BatchSize).ToListAsync(ct);

        foreach (var hardware in unacknowledged.Concat(actionable))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (hardware.StorageAckSentAtUtc is null)
                    await storage.AcknowledgeAsync(hardware, ct);
                else
                    await ProcessAsync(hardware, ct);
            }
            catch (Exception error) when (!ct.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(60, 1 << Math.Min(hardware.ProcessingAttempts + 1, 6)));
                var retryAt = time.GetUtcNow().UtcDateTime.Add(delay);
                await db.ParkingHardwareEvents.Where(x => x.Id == hardware.Id && x.RowVersion == hardware.RowVersion)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(x => x.ProcessingAttempts, x => x.ProcessingAttempts + 1)
                        .SetProperty(x => x.RetryAfterUtc, retryAt), ct);
                logger.LogWarning(error, "Parking event processing will retry. EventRecordId={EventRecordId}, " +
                    "ControllerId={ControllerId}, ProcessingStatus={ProcessingStatus}, RetryAfterUtc={RetryAfterUtc}",
                    hardware.Id, hardware.ControllerId, hardware.ProcessingStatus, retryAt);
            }
        }
    }

    private async Task ProcessAsync(ParkingHardwareEvent hardware, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<NParkDBContext>();
        var operations = provider.GetRequiredService<GateOperationService>();
        using var logging = logger.BeginScope(new Dictionary<string, object>
        {
            ["ControllerId"] = hardware.ControllerId,
            ["EventId"] = Convert.ToHexString(hardware.EventId),
            ["EventRecordId"] = hardware.Id,
            ["ReceivedAtUtc"] = hardware.ReceivedAtUtc
        });

        switch (hardware.ProcessingStatus)
        {
            case ParkingEventProcessingStatus.Received:
                var issuance = await provider.GetRequiredService<ISender>().Send(new IssueAutomaticVisitorTicket(hardware.Id), ct);
                if (issuance.IsFailure)
                    throw new InvalidOperationException(string.Join(",", issuance.Errors.Select(x => x.Code)));
                if (issuance.Value is { Ticket: { } ticket, Gate: { } gate } result)
                {
                    logger.LogInformation("Automatic visitor ticket issued. GateId={GateId}, TicketId={TicketId}, " +
                        "LprCaptureId={LprCaptureId}, CardOnly={CardOnly}", gate.Id, ticket.Id, result.LprCaptureId,
                        result.LprCaptureId is null);
                    await provider.GetRequiredService<TicketIssuanceService>().NotifyTicketAddedAsync(ticket, gate, isAutomatic: true)
                        .WaitAsync(TimeSpan.FromSeconds(1), ct);
                }
                else if (issuance.Value.ReasonCode is { } reason)
                    logger.LogInformation("Parking event did not issue a visitor ticket. ReasonCode={ReasonCode}", reason);
                break;

            case ParkingEventProcessingStatus.TicketCreated:
                var user = await db.Set<Ticket>().AsNoTracking().Where(x => x.Id == hardware.TicketId)
                    .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(ct);
                if (user is null)
                {
                    await FinishAsync(db, hardware, ParkingEventProcessingStatus.Failed, "HardwareTicketUnavailable", ct);
                    return;
                }

                var authorization = await operations.AuthorizeHardwareEntryAsync(hardware.Id, hardware.TicketId!.Value,
                    hardware.GateId!.Value, (ushort)hardware.ControllerId, user.Value, ct);
                if (authorization.IsFailure)
                {
                    if (authorization.Errors.Any(x => x.Type is ErrorType.Infrastructure or ErrorType.Unknown ||
                        x.Code == ErrorCodes.Common.Conflict))
                        throw new InvalidOperationException(string.Join(",", authorization.Errors.Select(x => x.Code)));
                    await FinishAsync(db, hardware, ParkingEventProcessingStatus.Failed, authorization.Errors.First().Code, ct);
                }
                break;

            case ParkingEventProcessingStatus.GateOperationCreated:
                var operation = await operations.GetAsync(hardware.GateOperationId!.Value, hardware.GateId, ct);
                if (operation.IsSuccess && operation.Value.Status == "Completed")
                    await FinishAsync(db, hardware, ParkingEventProcessingStatus.Completed, null, ct);
                else if (operation.IsSuccess && operation.Value.Status is "Failed" or "TimedOut" or "Unknown")
                    await FinishAsync(db, hardware, ParkingEventProcessingStatus.Failed, operation.Value.ReasonCode, ct);
                break;
        }
    }

    private async Task FinishAsync(NParkDBContext db, ParkingHardwareEvent hardware, ParkingEventProcessingStatus status,
        string? reason, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var changed = await db.ParkingHardwareEvents.Where(x => x.Id == hardware.Id && x.ProcessingStatus == hardware.ProcessingStatus)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.ProcessingStatus, status)
                .SetProperty(x => x.FailureReasonCode, reason).SetProperty(x => x.ProcessedAtUtc, now), ct);
        if (changed != 0)
            logger.LogInformation("Automatic visitor entry finished. GateId={GateId}, TicketId={TicketId}, " +
                "GateOperationId={GateOperationId}, ProcessingStatus={ProcessingStatus}, ReasonCode={ReasonCode}",
                hardware.GateId, hardware.TicketId, hardware.GateOperationId, status, reason);
    }
}
