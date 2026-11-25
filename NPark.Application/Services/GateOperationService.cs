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
    bool RetryAllowed, bool RequiresReconciliation, string? ReasonCode, string Topic, string Payload)
{
    public string? ReasonMessage => ReasonCode is null ? null : WorkflowMessages.Get(ReasonCode);
}

public sealed class GateOperationService(IUnitOfWork db, IServiceScopeFactory scopes,
    IOptions<GateOperationOptions> options, ILogger<GateOperationService> logger)
{
    public async Task<Result<GateOperationResponse>> AuthorizeAsync(
        Guid? ticketId, Guid? gateId, ushort? controller, Guid? userId, GateOperationType type, CancellationToken ct,
        Guid? requestId = null)
    {
        if (requestId == Guid.Empty)
            return Result<GateOperationResponse>.Fail(new Error("InvalidGateRequestId", WorkflowMessages.Get("InvalidGateRequestId"),
                ErrorType.Validation));

        var id = requestId ?? Guid.NewGuid();
        await using var phase = scopes.CreateAsyncScope();
        var sender = phase.ServiceProvider.GetRequiredService<ISender>();
        var operationDb = phase.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var request = new AuthorizeGateOperation(id, ticketId, gateId, controller, userId, type, requestId.HasValue);
        var result = await sender.Send(request, ct);
        if (result.IsFailure)
        {
            // A concurrent request can win the reservation after this request reads eligibility.
            if (result.Errors.Any(e => e.Type is ErrorType.Infrastructure or ErrorType.Unknown ||
                e.Code is "GateOperationPending" or ErrorCodes.Common.Conflict))
            {
                await using var read = scopes.CreateAsyncScope();
                var freshDb = read.ServiceProvider.GetRequiredService<IUnitOfWork>();
                if (request.HasRequestId && await freshDb.Repository<GateOperation>().GetByIdAsync(id, ct) is { } replay)
                    return PriorResponse(replay, AuthorizeGateOperationHandler.CheckReplay(replay, request));
                if (await AuthorizeGateOperationHandler.ResolveGateAsync(freshDb, request, ct) is { } mapping)
                {
                    var existing = ticketId is Guid ticket ? await TicketExitEligibilityService.FindReservationAsync(freshDb, ticket, ct) : null;
                    if (existing is not null)
                        return PriorResponse(existing, AuthorizeGateOperationHandler.CheckPrior(existing, mapping.Gate.Id,
                            mapping.Controller, type, request.HasRequestId, logger));
                    if (await AuthorizeGateOperationHandler.FindControllerReservationAsync(freshDb, mapping.Controller, ct) is { } reserved)
                        return type == GateOperationType.Manual
                            ? PriorResponse(reserved, AuthorizeGateOperationHandler.CheckPrior(reserved, mapping.Gate.Id,
                                mapping.Controller, type, request.HasRequestId, logger))
                            : Result<GateOperationResponse>.Fail(TicketExitEligibilityService.AsError("GateOperationPending"));
                }
            }
            logger.LogInformation("Gate authorization declined for {OperationType}, ticket {TicketId}, gate {GateId}, " +
                "requested controller {ControllerId}, reasons {Reasons}", type, ticketId, gateId, controller,
                string.Join(",", result.Errors.Select(e => e.Code)));
            return Result<GateOperationResponse>.Fail(result.Errors);
        }
        var operation = await operationDb.Repository<GateOperation>().GetByIdAsync(result.Value.OperationId, ct);
        if (operation is not null && result.Value.Created)
            logger.LogInformation("Gate operation authorized. OperationId={OperationId}, CommandId={CommandId}, " +
                "ControllerId={ControllerId}, TicketId={TicketId}, GateId={GateId}, NewStatus={NewStatus}, OperationType={OperationType}",
                operation.Id, operation.CommandId, operation.ControllerId, operation.TicketIdSnapshot,
                operation.GateId, operation.Status, operation.Type);
        return operation is null
            ? Result<GateOperationResponse>.Fail(new Error("GateOperationNotFound", "Gate operation not found.", ErrorType.NotFound))
            : Result<GateOperationResponse>.Ok(ToResponse(operation));
    }

    private Result<GateOperationResponse> PriorResponse(GateOperation operation, Result<GateAuthorization> prior) =>
        prior.IsSuccess ? Result<GateOperationResponse>.Ok(ToResponse(operation)) : Result<GateOperationResponse>.Fail(prior.Errors);

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
        return new(operation.Id, operation.TicketIdSnapshot, operation.GateId,
            operation.CommandId.ToString(CultureInfo.InvariantCulture), operation.ControllerId,
            operation.Type.ToString(), operation.Status.ToString(), operation.AckStatusCode == 0,
            completedExit, completedExit, AsUtc(operation.AcknowledgedAtUtc), AsUtc(operation.PublishedAtUtc),
            operation.AckStatusCode, AsUtc(operation.CreatedAtUtc)!.Value, AsUtc(operation.CompletedAtUtc),
            operation.Status == GateOperationStatus.Acknowledged, false, operation.ErrorCode,
            options.Value.CommandTopic(operation.ControllerId),
            Convert.ToHexString(GatePacketCodec.EncodeOpen((ushort)operation.ControllerId, (ulong)operation.CommandId)));
    }

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
    ushort? Controller, Guid? UserId, GateOperationType Type, bool HasRequestId) : ITransactionalCommand<GateAuthorization>;

internal sealed record GateAuthorization(Guid OperationId, bool Created = false);

internal sealed class AuthorizeGateOperationHandler(IUnitOfWork db, ITicketExitEligibilityService eligibility,
    ParkingBusinessClock clock, ILogger<AuthorizeGateOperationHandler> logger) : ICommandHandler<AuthorizeGateOperation, GateAuthorization>
{
    public async Task<Result<GateAuthorization>> Handle(AuthorizeGateOperation command, CancellationToken ct)
    {
        if (command.HasRequestId && await db.Repository<GateOperation>().GetByIdAsync(command.OperationId, ct) is { } replay)
            return CheckReplay(replay, command);
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
                return CheckPrior(prior, gate.Id, numeric, command.Type, command.HasRequestId, logger);

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

        var reserved = await FindControllerReservationAsync(db, numeric, ct);
        if (reserved is not null)
            return command.Type == GateOperationType.Manual
                ? CheckPrior(reserved, gate.Id, numeric, command.Type, command.HasRequestId, logger) : Fail("GateOperationPending");

        await db.Repository<GateOperation>().AddAsync(new GateOperation(command.OperationId,
            command.TicketId, gate.Id, command.UserId, numeric, command.Type, clock.UtcNow), ct);
        return Result<GateAuthorization>.Ok(new(command.OperationId, Created: true));
    }

    internal static Task<GateOperation?> FindControllerReservationAsync(IUnitOfWork db, ushort controller, CancellationToken ct) =>
        db.FirstOrDefaultWithSpecAsync(new Matching<GateOperation>(x => x.ReservedControllerId == controller &&
            (x.Status == GateOperationStatus.Pending || x.Status == GateOperationStatus.Published)), ct);

    internal static Result<GateAuthorization> CheckReplay(GateOperation prior, AuthorizeGateOperation request) =>
        prior.TicketIdSnapshot == request.TicketId && prior.GateId == request.GateId && prior.UserId == request.UserId &&
        prior.Type == request.Type && (!request.Controller.HasValue || prior.ControllerId == request.Controller.Value)
            ? Result<GateAuthorization>.Ok(new(prior.Id)) : Fail("GateRequestIdConflict");

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

    internal static Result<GateAuthorization> CheckPrior(GateOperation prior, Guid gateId, ushort controller, GateOperationType type,
        bool hasRequestId, ILogger logger)
    {
        var sameRequest = prior.GateId == gateId && prior.Type == type;
        if (sameRequest && prior.Status == GateOperationStatus.Completed)
            return Result<GateAuthorization>.Ok(new(prior.Id));

        var changedController = sameRequest && prior.ControllerId != controller;
        var reason = changedController ? "GateOperationControllerChanged" : "GateOperationPending";
        if (changedController || !sameRequest)
            logger.LogWarning("Existing gate operation blocks a new command. OperationId={OperationId}, TicketId={TicketId}, " +
                "GateId={GateId}, CommandId={CommandId}, ControllerId={ControllerId}, CurrentGateControllerId={CurrentControllerId}, " +
                "Status={Status}, Reason={Reason}, ReservedTicketId={ReservedTicketId}, ReservedControllerId={ReservedControllerId}, " +
                "DispatchStartedAtUtc={DispatchStartedAtUtc}, PublishedAtUtc={PublishedAtUtc}, AcknowledgedAtUtc={AcknowledgedAtUtc}",
                prior.Id, prior.TicketIdSnapshot, prior.GateId, prior.CommandId, prior.ControllerId, controller,
                prior.Status, reason, prior.ReservedTicketId, prior.ReservedControllerId, prior.DispatchStartedAtUtc,
                prior.PublishedAtUtc, prior.AcknowledgedAtUtc);
        if (changedController)
            return Result<GateAuthorization>.Fail(Error.Conflict(reason, string.Format(CultureInfo.CurrentCulture,
                WorkflowMessages.Get(reason), prior.Id, prior.ControllerId, controller, prior.Status)));
        return sameRequest && !hasRequestId ? Result<GateAuthorization>.Ok(new(prior.Id)) : Fail("GateOperationPending");
    }

    private static Result<GateAuthorization> Fail(string reason) => Result<GateAuthorization>.Fail(TicketExitEligibilityService.AsError(reason));
}
