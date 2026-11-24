using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Options;
using NPark.Application.Options;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Application.Services;

public enum GateTransition { Dispatch, Retry, Published, DeliveryUnknown, CheckTimeout, Complete, ReconcileNonExecution }

public sealed record AdvanceGateOperation(Guid OperationId, GateTransition Transition,
    Guid? GateId = null, Guid? ActorId = null, string? Evidence = null) : ITransactionalCommand<GateTransitionResult>;
public sealed record RecordGateAcknowledgment(GateAck Ack) : ITransactionalCommand<GateTransitionResult>;
public sealed record GateTransitionResult(GateOperationResponse? Operation, TicketExitedNotification? Exit = null);

public sealed class GateOperationTransitions(IUnitOfWork db, ParkingBusinessClock clock,
    IOptions<GateOperationOptions> options, GateOperationService operations, ITicketArchiveService archives)
    : ICommandHandler<AdvanceGateOperation, GateTransitionResult>,
      ICommandHandler<RecordGateAcknowledgment, GateTransitionResult>
{
    public async Task<Result<GateTransitionResult>> Handle(RecordGateAcknowledgment command, CancellationToken ct)
    {
        var ack = command.Ack;
        if (ack.CommandId > long.MaxValue)
            return Unchanged();
        var operation = await db.FirstOrDefaultWithSpecAsync(new Matching<GateOperation>(x =>
            x.CommandId == (long)ack.CommandId && x.ControllerId == ack.ControllerId), ct);
        return operation != null && operation.Acknowledge(ack.Status, clock.UtcNow)
            ? Changed(operation) : Unchanged();
    }

    public async Task<Result<GateTransitionResult>> Handle(AdvanceGateOperation command, CancellationToken ct)
    {
        var operation = await db.Repository<GateOperation>().GetByIdAsync(command.OperationId, ct);
        if (operation is null || command.GateId.HasValue && operation.GateId != command.GateId)
            return Result<GateTransitionResult>.Fail(new Error("GateOperationNotFound", "Gate operation not found.", ErrorType.NotFound));

        var now = clock.UtcNow;
        switch (command.Transition)
        {
            case GateTransition.Dispatch when operation.Status == GateOperationStatus.Pending &&
                !operation.DispatchStartedAtUtc.HasValue:
                operation.BeginDispatch(now, retry: false);
                return Changed(operation);
            case GateTransition.Retry when GateOperationService.CanRetry(operation, options.Value):
                operation.BeginDispatch(now, retry: true);
                return Changed(operation);
            case GateTransition.Published when operation.DispatchStartedAtUtc.HasValue:
                operation.MarkPublished(now);
                return Changed(operation);
            case GateTransition.DeliveryUnknown when operation.Status == GateOperationStatus.Pending:
                operation.MarkUncertain(timeout: false);
                return Changed(operation);
            case GateTransition.CheckTimeout when operation.Status is GateOperationStatus.Pending or GateOperationStatus.Published:
                var attempted = operation.PublishedAtUtc ?? operation.DispatchStartedAtUtc;
                if (attempted.HasValue && attempted.Value.AddSeconds(options.Value.AckTimeoutSeconds) <= now)
                {
                    operation.MarkUncertain(timeout: operation.PublishedAtUtc.HasValue);
                    return Changed(operation);
                }
                break;
            case GateTransition.Complete when operation.Status == GateOperationStatus.Acknowledged:
                return await CompleteAsync(operation, now, ct);
            case GateTransition.ReconcileNonExecution when command.ActorId is Guid actor && !string.IsNullOrWhiteSpace(command.Evidence)
                && operation.Status is GateOperationStatus.TimedOut or GateOperationStatus.Unknown:
                operation.ReconcileNonExecution(actor, command.Evidence, now);
                return Changed(operation);
        }
        return Unchanged();
    }

    private async Task<Result<GateTransitionResult>> CompleteAsync(GateOperation operation, DateTime now, CancellationToken ct)
    {
        TicketExitedNotification? notification = null;
        if (operation.Type == GateOperationType.Exit)
        {
            var archive = await archives.CompleteAsync(operation, ct);
            var gate = await db.Repository<ParkingGate>().GetByIdAsync(operation.GateId, ct);
            notification = new()
            {
                TicketId = archive.Id,
                GateId = operation.GateId,
                UserId = operation.UserId ?? archive.UserId,
                ExitDate = archive.EndDate,
                GateNumber = gate?.GateNumber.ToString() ?? string.Empty
            };
        }
        else if (operation.Type == GateOperationType.Entry)
        {
            var ticket = await db.Repository<Ticket>().GetByIdAsync(operation.TicketIdSnapshot!.Value, ct)
                ?? throw new InvalidOperationException("The acknowledged entry ticket is missing.");
            ticket.ConfirmEntry(DateTime.SpecifyKind(operation.AcknowledgedAtUtc!.Value, DateTimeKind.Utc));
        }
        operation.Complete(now);
        return Result<GateTransitionResult>.Ok(new(operations.ToResponse(operation), notification));
    }

    private Result<GateTransitionResult> Changed(GateOperation operation) =>
        Result<GateTransitionResult>.Ok(new(operations.ToResponse(operation)));

    private static Result<GateTransitionResult> Unchanged() => Result<GateTransitionResult>.Ok(new(null));
}
