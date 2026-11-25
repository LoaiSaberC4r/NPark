using System.Globalization;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPark.Application.Options;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Application.Services;

public sealed record GateOperationResponse(
    Guid OperationId, Guid? TicketId, Guid GateId, string CommandId, int ControllerId,
    string OperationType, string Status, bool IsAcknowledged, bool TicketExited, bool Archived,
    DateTime? AcknowledgedAtUtc, DateTime? PublishedAtUtc, byte? AckStatusCode,
    DateTime CreatedAtUtc, DateTime? CompletedAtUtc,
    bool RetryAllowed, bool RequiresReconciliation, string? ReasonCode, string Topic, string Payload);

public sealed class GateOperationService(IUnitOfWork db, IServiceScopeFactory scopes,
    IOptions<GateOperationOptions> options, ILogger<GateOperationService> logger)
{
    public async Task<Result<GateOperationResponse>> AuthorizeAsync(
        Guid? ticketId, Guid? gateId, ushort? controller, Guid? userId, GateOperationType type, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await using var phase = scopes.CreateAsyncScope();
        var sender = phase.ServiceProvider.GetRequiredService<ISender>();
        var operationDb = phase.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var request = new AuthorizeGateOperation(id, ticketId, gateId, controller, userId, type);
        var result = await sender.Send(request, ct);
        if (result.IsFailure)
        {
            // A concurrent request can win the reservation after this request reads eligibility.
            var existing = ticketId.HasValue && result.Errors.Any(e => e.Type is ErrorType.Infrastructure or ErrorType.Unknown ||
                e.Code is "GateOperationPending" or ErrorCodes.Common.Conflict)
                ? await TicketExitEligibilityService.FindReservationAsync(operationDb, ticketId.Value, ct)
                : null;
            if (existing is not null &&
                await AuthorizeGateOperationHandler.ResolveGateAsync(operationDb, request, ct) is { } mapping)
            {
                var prior = AuthorizeGateOperationHandler.CheckPrior(existing, mapping.Gate.Id, mapping.Controller, type, logger);
                return prior.IsSuccess ? Result<GateOperationResponse>.Ok(ToResponse(existing))
                    : Result<GateOperationResponse>.Fail(prior.Errors);
            }
            logger.LogInformation("Gate authorization declined for {OperationType}, ticket {TicketId}, gate {GateId}, " +
                "requested controller {ControllerId}, reasons {Reasons}", type, ticketId, gateId, controller,
                string.Join(",", result.Errors.Select(e => e.Code)));
            return Result<GateOperationResponse>.Fail(result.Errors);
        }
        var operation = await operationDb.Repository<GateOperation>().GetByIdAsync(result.Value, ct);
        if (operation?.Id == id)
            logger.LogInformation("Gate operation created. OperationId={OperationId}, CommandId={CommandId}, " +
                "ControllerId={ControllerId}, TicketId={TicketId}, GateId={GateId}, Status={Status}",
                operation.Id, operation.CommandId, operation.ControllerId, operation.TicketIdSnapshot,
                operation.GateId, operation.Status);
        return operation is null
            ? Result<GateOperationResponse>.Fail(new Error("GateOperationNotFound", "Gate operation not found.", ErrorType.NotFound))
            : Result<GateOperationResponse>.Ok(ToResponse(operation));
    }

    public async Task<Result<GateOperationResponse>> GetAsync(Guid operationId, Guid? gateId, CancellationToken ct)
    {
        var operation = await db.Repository<GateOperation>().GetByIdAsync(operationId, ct);
        return operation is null || gateId.HasValue && operation.GateId != gateId
            ? Result<GateOperationResponse>.Fail(new Error("GateOperationNotFound", "Gate operation not found.", ErrorType.NotFound))
            : Result<GateOperationResponse>.Ok(ToResponse(operation));
    }

    public async Task<IReadOnlyList<GateOperationResponse>> ListAsync(Guid gateId, Guid? ticketId,
        int? controller, GateOperationStatus? status, CancellationToken ct)
    {
        var operations = await db.ListWithSpecAsync(new GateOperationHistorySpec(gateId, ticketId, controller, status), ct);
        return operations.Select(ToResponse).ToArray();
    }

    public GateOperationResponse ToResponse(GateOperation operation)
    {
        var completedExit = operation.Type == GateOperationType.Exit && operation.Status == GateOperationStatus.Completed;
        var uncertain = operation.Status is GateOperationStatus.TimedOut or GateOperationStatus.Unknown;
        return new(operation.Id, operation.TicketIdSnapshot, operation.GateId,
            operation.CommandId.ToString(CultureInfo.InvariantCulture), operation.ControllerId,
            operation.Type.ToString(), operation.Status.ToString(), operation.AckStatusCode == 0,
            completedExit, completedExit, AsUtc(operation.AcknowledgedAtUtc), AsUtc(operation.PublishedAtUtc),
            operation.AckStatusCode, AsUtc(operation.CreatedAtUtc)!.Value, AsUtc(operation.CompletedAtUtc),
            operation.Status == GateOperationStatus.Acknowledged || CanRetry(operation, options.Value),
            uncertain && !CanRetry(operation, options.Value), operation.ErrorCode,
            options.Value.CommandTopic(operation.ControllerId),
            Convert.ToHexString(GatePacketCodec.EncodeOpen((ushort)operation.ControllerId, (ulong)operation.CommandId)));
    }

    public static bool CanRetry(GateOperation op, GateOperationOptions options) =>
        options.HardwareDeduplicationConfirmed && op.RetryCount < options.MaxRetries &&
        op.Status is GateOperationStatus.TimedOut or GateOperationStatus.Unknown;

    private static DateTime? AsUtc(DateTime? time) =>
        time.HasValue ? DateTime.SpecifyKind(time.Value, DateTimeKind.Utc) : null;

    private sealed class GateOperationHistorySpec : BuildingBlock.Domain.Specification.Specification<GateOperation>
    {
        public GateOperationHistorySpec(Guid gateId, Guid? ticketId, int? controller, GateOperationStatus? status)
        {
            AddCriteria(x => x.GateId == gateId && (!ticketId.HasValue || x.TicketIdSnapshot == ticketId)
                && (!controller.HasValue || x.ControllerId == controller) && (!status.HasValue || x.Status == status));
            AddOrderByDescending(x => x.CreatedAtUtc);
            ApplyPaging(1, 100);
        }
    }
}

internal sealed record AuthorizeGateOperation(Guid OperationId, Guid? TicketId, Guid? GateId,
    ushort? Controller, Guid? UserId, GateOperationType Type) : ITransactionalCommand<Guid>;

internal sealed class AuthorizeGateOperationHandler(IUnitOfWork db, ITicketExitEligibilityService eligibility,
    ParkingBusinessClock clock, ILogger<AuthorizeGateOperationHandler> logger) : ICommandHandler<AuthorizeGateOperation, Guid>
{
    public async Task<Result<Guid>> Handle(AuthorizeGateOperation command, CancellationToken ct)
    {
        if (await ResolveGateAsync(db, command, ct) is not { } mapping)
            return Fail("InvalidExitGate");

        var (gate, numeric) = mapping;
        logger.LogInformation("Gate authorization requested. OperationType={OperationType}, TicketId={TicketId}, " +
            "GateId={GateId}, ControllerId={ControllerId}", command.Type, command.TicketId, gate.Id, numeric);

        if (command.TicketId is Guid ticketId)
        {
            var prior = await TicketExitEligibilityService.FindReservationAsync(db, ticketId, ct)
                ?? await db.FirstOrDefaultWithSpecAsync(new Matching<GateOperation>(x => x.TicketIdSnapshot == ticketId &&
                    x.Type == command.Type && x.Status == GateOperationStatus.Completed), ct);
            if (prior is not null)
                return CheckPrior(prior, gate.Id, numeric, command.Type, logger);

            var ticket = await db.Repository<Ticket>().GetByIdAsync(ticketId, ct);
            if (command.Type == GateOperationType.Exit)
            {
                var assessment = await eligibility.EvaluateAsync(ticket, gate.Id, ct);
                if (!assessment.IsValidForExit)
                    return Fail(assessment.ReasonCode);
            }
            else if (command.Type != GateOperationType.Entry || gate.GateType != GateType.Entrance ||
                ticket is null || ticket.GateId != gate.Id || ticket.EndDate.HasValue ||
                ticket.EntryConfirmationStatus != EntryConfirmationStatus.Pending ||
                (ticket.PricingModeSnapshot == PriceType.Enter && !ticket.IsCashierCollected))
                return Fail("EntryNotAuthorized");
        }
        else if (command.Type != GateOperationType.Manual)
            return Fail("TicketNotFound");

        var reserved = await db.FirstOrDefaultWithSpecAsync(
            new Matching<GateOperation>(x => x.ReservedControllerId == numeric), ct);
        if (reserved is not null)
            return command.Type == GateOperationType.Manual
                ? CheckPrior(reserved, gate.Id, numeric, command.Type, logger) : Fail("GateOperationPending");

        await db.Repository<GateOperation>().AddAsync(new GateOperation(command.OperationId,
            command.TicketId, gate.Id, command.UserId, numeric, command.Type, clock.UtcNow), ct);
        return Result<Guid>.Ok(command.OperationId);
    }

    internal static async Task<(ParkingGate Gate, ushort Controller)?> ResolveGateAsync(
        IUnitOfWork db, AuthorizeGateOperation command, CancellationToken ct)
    {
        var gate = command.GateId is Guid gateId
            ? await db.Repository<ParkingGate>().GetByIdAsync(gateId, ct)
            : null;
        var controller = command.Controller;
        if (command.GateId.HasValue)
        {
            if (gate is null || !ushort.TryParse(gate.ControlIp, NumberStyles.None, CultureInfo.InvariantCulture, out var mapped) ||
                controller.HasValue && controller.Value != mapped)
                return null;
            controller = mapped;
        }
        if (controller is null or 0)
            return null;

        var registered = controller.Value.ToString(CultureInfo.InvariantCulture);
        var matches = await db.ListWithSpecAsync(new Matching<ParkingGate>(x => x.ControlIp == registered, 2), ct);
        if (matches.Count != 1 || gate is not null && matches[0].Id != gate.Id)
            return null;
        return (matches[0], controller.Value);
    }

    internal static Result<Guid> CheckPrior(GateOperation prior, Guid gateId, ushort controller, GateOperationType type,
        ILogger logger)
    {
        var sameRequest = prior.GateId == gateId && prior.Type == type;
        if (sameRequest && prior.Status == GateOperationStatus.Completed)
            return Result<Guid>.Ok(prior.Id);

        var changedController = sameRequest && prior.ControllerId != controller;
        var reason = changedController ? "GateOperationControllerChanged" : prior.ErrorCode ?? "GateOperationPending";
        if (changedController || !sameRequest || prior.Status is GateOperationStatus.TimedOut or GateOperationStatus.Unknown)
            logger.LogWarning("Existing gate operation blocks a new command. OperationId={OperationId}, TicketId={TicketId}, " +
                "GateId={GateId}, CommandId={CommandId}, ControllerId={ControllerId}, CurrentGateControllerId={CurrentControllerId}, " +
                "Status={Status}, Reason={Reason}, ReservedTicketId={ReservedTicketId}, ReservedControllerId={ReservedControllerId}, " +
                "DispatchStartedAtUtc={DispatchStartedAtUtc}, PublishedAtUtc={PublishedAtUtc}, AcknowledgedAtUtc={AcknowledgedAtUtc}",
                prior.Id, prior.TicketIdSnapshot, prior.GateId, prior.CommandId, prior.ControllerId, controller,
                prior.Status, reason, prior.ReservedTicketId, prior.ReservedControllerId, prior.DispatchStartedAtUtc,
                prior.PublishedAtUtc, prior.AcknowledgedAtUtc);
        if (changedController)
            return Result<Guid>.Fail(Error.Conflict(reason, string.Format(CultureInfo.CurrentCulture,
                WorkflowMessages.Get(reason), prior.Id, prior.ControllerId, controller, prior.Status)));
        return sameRequest ? Result<Guid>.Ok(prior.Id) : Fail("GateOperationPending");
    }

    private static Result<Guid> Fail(string reason) => Result<Guid>.Fail(TicketExitEligibilityService.AsError(reason));
}
