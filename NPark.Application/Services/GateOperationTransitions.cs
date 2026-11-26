using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPark.Application.Options;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Application.Services;

public enum GateTransition { Dispatch, Published, DeliveryUnknown, CheckTimeout, Complete, ReconcileNonExecution }

public sealed record AdvanceGateOperation(Guid OperationId, GateTransition Transition,
    Guid? GateId = null, Guid? ActorId = null, string? Evidence = null) : ITransactionalCommand<GateTransitionResult>;
public sealed record RecordGateAcknowledgment(GateAck Ack) : ITransactionalCommand<GateTransitionResult>;
public sealed record GateTransitionResult(GateOperationResponse? Operation, TicketExitedNotification? Exit = null,
    GateOperationStatus? PreviousStatus = null, bool ControllerReleased = false, bool TicketReleased = false,
    DateTime? DispatchAttemptDeadlineUtc = null);

public sealed class GateOperationTransitions(IUnitOfWork db, ParkingBusinessClock clock,
    IOptions<GateOperationOptions> options, GateOperationService operations, ITicketArchiveService archives,
    ILogger<GateOperationTransitions> logger)
    : ICommandHandler<AdvanceGateOperation, GateTransitionResult>,
      ICommandHandler<RecordGateAcknowledgment, GateTransitionResult>
{
    public async Task<Result<GateTransitionResult>> Handle(RecordGateAcknowledgment command, CancellationToken ct)
    {
        var ack = command.Ack;
        if (ack.CommandId is 0 or > long.MaxValue || ack.ControllerId == 0 || ack.Status > 3)
            return IgnoreAck(ack, "InvalidFrameFields");
        var operation = await db.FirstOrDefaultWithSpecAsync(new Matching<GateOperation>(x =>
            x.CommandId == (long)ack.CommandId && x.ControllerId == ack.ControllerId), ct);
        if (operation is null)
            return IgnoreAck(ack, "NoMatchingOperation");
        if (operation.CompletionMode != GateOperationCompletionMode.Hardware)
            return IgnoreAck(ack, "OperationNotAwaitingAck", operation);

        var previous = Snapshot(operation);
        if (operation.Status == GateOperationStatus.Acknowledged)
            return IgnoreAck(ack, operation.AckStatusCode == ack.Status ? "Duplicate" : "OperationNotAwaitingAck", operation);
        if (operation.Status == GateOperationStatus.Completed && operation.AckStatusCode == ack.Status)
            return IgnoreAck(ack, "Duplicate", operation);
        if (operation.Status is not (GateOperationStatus.Pending or GateOperationStatus.Published))
            return IgnoreAck(ack, "LateAckForTerminalOperation", operation);
        if (operation.DispatchStartedAtUtc is not DateTime started)
            return IgnoreAck(ack, "OperationNotAwaitingAck", operation);

        var now = clock.UtcNow;
        if (now >= AckDeadline(operation.PublishedAtUtc ?? started))
        {
            // An in-flight publish owns its reservation until settled. Recovery checks the
            // dispatch lock before expiring a claimed Pending operation.
            if (operation.Status == GateOperationStatus.Pending)
                return IgnoreAck(ack, "AckDeadlineExpired", operation);
            operation.MarkUncertain(timeout: true);
            IgnoreAck(ack, "AckDeadlineExpired", operation);
            return Changed(operation, previous);
        }

        return operation.Acknowledge(ack.Status, now)
            ? Changed(operation, previous) : IgnoreAck(ack, "OperationNotAwaitingAck", operation);
    }

    public async Task<Result<GateTransitionResult>> Handle(AdvanceGateOperation command, CancellationToken ct)
    {
        var operation = await db.Repository<GateOperation>().GetByIdAsync(command.OperationId, ct);
        if (operation is null || command.GateId.HasValue && operation.GateId != command.GateId)
            return Result<GateTransitionResult>.Fail(new Error("GateOperationNotFound", "Gate operation not found.", ErrorType.NotFound));
        if (operation.CompletionMode != GateOperationCompletionMode.Hardware)
            return Unchanged();

        var previous = Snapshot(operation);
        var now = clock.UtcNow;
        var dispatchDeadline = operation.CreatedAtUtc.AddSeconds(options.Value.DispatchWaitTimeoutSeconds);
        var exitMqttDisabled = operation.Type == GateOperationType.Exit && !options.Value.EnableExitGateMqtt;
        switch (command.Transition)
        {
            case GateTransition.Dispatch when operation.Status == GateOperationStatus.Pending &&
                !operation.DispatchStartedAtUtc.HasValue && !exitMqttDisabled:
                if (now >= dispatchDeadline)
                {
                    operation.MarkDispatchExpired();
                    return Changed(operation, previous);
                }
                operation.BeginDispatch(now);
                return Changed(operation, previous, attemptDeadline: AckDeadline(now));
            case GateTransition.Published when operation.Status == GateOperationStatus.Pending &&
                operation.DispatchStartedAtUtc is DateTime started:
                if (now >= AckDeadline(started))
                    operation.MarkUncertain(timeout: false);
                else
                    operation.MarkPublished(now);
                return Changed(operation, previous);
            case GateTransition.DeliveryUnknown when operation.Status == GateOperationStatus.Pending &&
                operation.DispatchStartedAtUtc.HasValue:
                operation.MarkUncertain(timeout: false);
                return Changed(operation, previous);
            case GateTransition.CheckTimeout when operation.Status is GateOperationStatus.Pending or GateOperationStatus.Published:
                var attempted = operation.PublishedAtUtc ?? operation.DispatchStartedAtUtc;
                if (attempted.HasValue && now >= AckDeadline(attempted.Value))
                    operation.MarkUncertain(timeout: operation.PublishedAtUtc.HasValue);
                else if (!attempted.HasValue && exitMqttDisabled)
                    operation.CancelUndispatchedHardwareExit();
                else if (!attempted.HasValue && now >= dispatchDeadline)
                    operation.MarkDispatchExpired();
                if (operation.Status != previous.Status)
                    return Changed(operation, previous);
                break;
            case GateTransition.Complete when operation.Status == GateOperationStatus.Acknowledged:
                return Changed(operation, previous, await CompleteAsync(operation, now, ct));
            case GateTransition.ReconcileNonExecution when command.ActorId is Guid actor && !string.IsNullOrWhiteSpace(command.Evidence)
                && operation.ReconciledAtUtc is null && operation.Status is (GateOperationStatus.TimedOut or GateOperationStatus.Unknown):
                operation.ReconcileNonExecution(actor, command.Evidence, now);
                return Changed(operation, previous);
        }
        return Unchanged();
    }

    private async Task<TicketExitedNotification?> CompleteAsync(GateOperation operation, DateTime now, CancellationToken ct)
    {
        TicketExitedNotification? notification = null;
        if (operation.Type == GateOperationType.Exit)
            notification = await archives.CompleteAsync(operation, ct);
        else if (operation.Type == GateOperationType.Entry)
        {
            var ticket = await db.Repository<Ticket>().GetByIdAsync(operation.TicketIdSnapshot!.Value, ct)
                ?? throw new InvalidOperationException("The acknowledged entry ticket is missing.");
            ticket.ConfirmEntry(DateTime.SpecifyKind(operation.AcknowledgedAtUtc!.Value, DateTimeKind.Utc));
        }
        operation.Complete(now);
        return notification;
    }

    private DateTime AckDeadline(DateTime start) => start.AddSeconds(options.Value.AckTimeoutSeconds);

    private static (GateOperationStatus Status, bool ControllerReserved, bool TicketReserved) Snapshot(GateOperation operation) =>
        (operation.Status, operation.ReservedControllerId.HasValue, operation.ReservedTicketId.HasValue);

    private Result<GateTransitionResult> Changed(GateOperation operation,
        (GateOperationStatus Status, bool ControllerReserved, bool TicketReserved) previous,
        TicketExitedNotification? exit = null, DateTime? attemptDeadline = null) =>
        Result<GateTransitionResult>.Ok(new(operations.ToResponse(operation), exit, previous.Status,
            previous.ControllerReserved && !operation.ReservedControllerId.HasValue,
            previous.TicketReserved && !operation.ReservedTicketId.HasValue, attemptDeadline));

    private Result<GateTransitionResult> IgnoreAck(GateAck ack, string reason, GateOperation? operation = null)
    {
        logger.LogWarning("Hardware ACK ignored. Reason={Reason}, ControllerId={ControllerId}, CommandId={CommandId}, " +
            "AckStatusCode={AckStatusCode}, OperationStatus={OperationStatus}, OperationId={OperationId}, TicketId={TicketId}",
            reason, ack.ControllerId, ack.CommandId, ack.Status, operation?.Status, operation?.Id, operation?.TicketIdSnapshot);
        return Unchanged();
    }

    private static Result<GateTransitionResult> Unchanged() => Result<GateTransitionResult>.Ok(new(null));
}
