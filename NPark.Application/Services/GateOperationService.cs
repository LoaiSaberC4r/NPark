using System.Globalization;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPark.Application.Options;
using NPark.Application.Shared.Dto;
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
    public bool IsAutomaticVisitorEntry { get; init; }
    public string CompletionMode { get; init; } = nameof(GateOperationCompletionMode.Hardware);
}

public sealed class GateOperationService(IUnitOfWork db, IServiceScopeFactory scopes,
    IOptions<GateOperationOptions> options, IOptions<SubscriberButtonLockOptions> lockOptions,
    ParkingBusinessClock clock, GateOperationNotifier notifications, ILogger<GateOperationService> logger)
{
    public Task<Result<GateOperationResponse>> AuthorizeAsync(
        Guid? ticketId, Guid? gateId, ushort? controller, Guid? userId, GateOperationType type, CancellationToken ct,
        Guid? requestId = null)
    {
        if (type == GateOperationType.LockCardButton)
            return Task.FromResult(Result<GateOperationResponse>.Fail(TicketExitEligibilityService.AsError("EntryNotAuthorized")));
        return AuthorizeCoreAsync(ticketId, gateId, controller, userId, type, ct, requestId);
    }

    internal Task<Result<GateOperationResponse>> AuthorizeSubscriberLockAsync(LprCapture capture, ushort controller,
        CancellationToken ct) => AuthorizeCoreAsync(null, capture.GateId, controller, null, GateOperationType.LockCardButton,
            ct, capture.Id);

    public Task<Result<GateOperationResponse>> AuthorizeHardwareEntryAsync(Guid eventId, Guid ticketId, Guid gateId,
        ushort controller, Guid userId, CancellationToken ct) => AuthorizeCoreAsync(ticketId, gateId, controller, userId,
            GateOperationType.Entry, ct, eventId, hardwareEntry: true);

    internal async Task<GateOperationResponse> WaitForSubscriberLockAsync(GateOperationResponse operation, CancellationToken ct)
    {
        var timing = options.Value;
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(TimeSpan.FromSeconds(
            timing.DispatchWaitTimeoutSeconds + timing.AckTimeoutSeconds + 2 * timing.RecoveryIntervalSeconds));
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(150));
            while (operation.Status is "Pending" or "Published" or "Acknowledged" &&
                await timer.WaitForNextTickAsync(budget.Token))
            {
                await using var scope = scopes.CreateAsyncScope();
                var current = await scope.ServiceProvider.GetRequiredService<GateOperationService>()
                    .GetAsync(operation.OperationId, operation.GateId, budget.Token);
                if (current.IsFailure)
                    break;
                operation = current.Value;
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && budget.IsCancellationRequested)
        {
            // The durable gate expiry worker owns reservation release.
        }
        return operation;
    }

    private async Task<Result<GateOperationResponse>> AuthorizeCoreAsync(
        Guid? ticketId, Guid? gateId, ushort? controller, Guid? userId, GateOperationType type, CancellationToken ct,
        Guid? requestId, bool hardwareEntry = false)
    {
        if (requestId == Guid.Empty)
            return Result<GateOperationResponse>.Fail(new Error("InvalidGateRequestId", WorkflowMessages.Get("InvalidGateRequestId"),
                ErrorType.Validation));

        var id = requestId ?? Guid.NewGuid();
        await using var phase = scopes.CreateAsyncScope();
        var sender = phase.ServiceProvider.GetRequiredService<ISender>();
        var operationDb = phase.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var request = new AuthorizeGateOperation(id, ticketId, gateId, controller, userId, type, requestId.HasValue, hardwareEntry);
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
                if (type == GateOperationType.LockCardButton &&
                    await freshDb.Repository<LprCapture>().GetByIdAsync(id, ct) is { } capture &&
                    await AuthorizeGateOperationHandler.FindRecentLockAsync(freshDb, capture,
                        clock.UtcNow.AddSeconds(-lockOptions.Value.DuplicateWindowSeconds), ct) is { } lockWinner)
                    return Result<GateOperationResponse>.Ok(ToResponse(lockWinner));
                if (await AuthorizeGateOperationHandler.ResolveGateAsync(freshDb, gateId, controller, ct) is { } mapping)
                {
                    var softwareExit = type == GateOperationType.Exit && !options.Value.EnableExitGateMqtt;
                    var existing = ticketId is Guid ticket ? await TicketExitEligibilityService.FindReservationAsync(freshDb, ticket, ct) : null;
                    if (existing is null && softwareExit && ticketId is Guid exitTicket)
                        existing = await AuthorizeGateOperationHandler.FindCompletedAsync(freshDb, exitTicket, type, ct);
                    if (existing is not null)
                        return PriorResponse(existing, AuthorizeGateOperationHandler.CheckPrior(existing, mapping.Gate.Id,
                            mapping.Controller, type, request.HasRequestId, logger));
                    if (!softwareExit && await AuthorizeGateOperationHandler.FindControllerReservationAsync(freshDb, mapping.Controller, ct) is { } reserved)
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
        if (operation is null)
            return Result<GateOperationResponse>.Fail(new Error("GateOperationNotFound", "Gate operation not found.", ErrorType.NotFound));

        var response = ToResponse(operation);
        if (result.Value.Exit is { } exit)
            await notifications.NotifyAsync(response, exit);
        return Result<GateOperationResponse>.Ok(response);
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
        var hardware = operation.CompletionMode == GateOperationCompletionMode.Hardware;
        return new GateOperationResponse(operation.Id, operation.TicketIdSnapshot, operation.GateId,
            operation.CommandId.ToString(CultureInfo.InvariantCulture), operation.ControllerId,
            operation.Type.ToString(), operation.Status.ToString(), hardware && operation.AckStatusCode == 0,
            completedExit, completedExit, AsUtc(operation.AcknowledgedAtUtc), AsUtc(operation.PublishedAtUtc),
            operation.AckStatusCode, AsUtc(operation.CreatedAtUtc)!.Value, AsUtc(operation.CompletedAtUtc),
            operation.Status == GateOperationStatus.Acknowledged, false, operation.ErrorCode,
            hardware ? options.Value.CommandTopic(operation.ControllerId) : string.Empty,
            hardware ? Convert.ToHexString(GatePacketCodec.EncodeOperation(operation.Type, (ushort)operation.ControllerId,
                (ulong)operation.CommandId)) : string.Empty)
        {
            IsAutomaticVisitorEntry = operation.IsAutomaticVisitorEntry,
            CompletionMode = operation.CompletionMode.ToString()
        };
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
    ushort? Controller, Guid? UserId, GateOperationType Type, bool HasRequestId, bool HardwareEntry = false)
    : ITransactionalCommand<GateAuthorization>;

internal sealed record GateAuthorization(Guid OperationId, bool Created = false, TicketExitedNotification? Exit = null);

internal sealed class AuthorizeGateOperationHandler(IUnitOfWork db, ITicketExitEligibilityService eligibility,
    SubscriberEligibilityService subscribers, ITicketArchiveService archives, IOptions<GateOperationOptions> options,
    IOptions<SubscriberButtonLockOptions> lockOptions, IOptions<AutomaticVisitorEntryOptions> visitorOptions,
    ParkingBusinessClock clock, ILogger<AuthorizeGateOperationHandler> logger) : ICommandHandler<AuthorizeGateOperation, GateAuthorization>
{
    public async Task<Result<GateAuthorization>> Handle(AuthorizeGateOperation command, CancellationToken ct)
    {
        if (command.HasRequestId && await db.Repository<GateOperation>().GetByIdAsync(command.OperationId, ct) is { } replay)
            return CheckReplay(replay, command);
        if (await ResolveGateAsync(db, command.GateId, command.Controller, ct) is not { } mapping)
            return Fail("InvalidExitGate");

        var (gate, numeric) = mapping;
        logger.LogInformation("Gate authorization requested. OperationType={OperationType}, TicketId={TicketId}, " +
            "GateId={GateId}, ControllerId={ControllerId}", command.Type, command.TicketId, gate.Id, numeric);

        ParkingHardwareEvent? hardware = null;
        if (command.HardwareEntry)
        {
            hardware = await db.Repository<ParkingHardwareEvent>().GetByIdAsync(command.OperationId, ct);
            if (!visitorOptions.Value.Supports(numeric) || command.Type != GateOperationType.Entry ||
                hardware?.ProcessingStatus != ParkingEventProcessingStatus.TicketCreated ||
                hardware.TicketId != command.TicketId || hardware.GateId != gate.Id || hardware.ControllerId != numeric)
                return Fail("EntryNotAuthorized");
        }

        if (command.TicketId is Guid ticketId)
        {
            var prior = await TicketExitEligibilityService.FindReservationAsync(db, ticketId, ct)
                ?? await FindCompletedAsync(db, ticketId, command.Type, ct);
            if (prior is not null)
                return hardware is not null ? Fail("EntryNotAuthorized") :
                    CheckPrior(prior, gate.Id, numeric, command.Type, command.HasRequestId, logger);

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
                (ticket.PricingModeSnapshot == PriceType.Enter && !ticket.IsCashierCollected) ||
                (hardware is not null && (ticket.UserId != command.UserId || ticket.IsSubscriber ||
                    ticket.PricingModeSnapshot != PriceType.Exit || ticket.CardNumber != hardware.CardNumber)))
                return Fail("EntryNotAuthorized");
        }
        else if (command.Type == GateOperationType.LockCardButton)
        {
            var capture = await db.Repository<LprCapture>().GetByIdAsync(command.OperationId, ct);
            if (!command.HasRequestId || gate.GateType != GateType.Entrance || !lockOptions.Value.Supports(numeric) ||
                capture is null || capture.GateId != gate.Id || capture.ControllerId != numeric)
                return Fail("EntryNotAuthorized");

            var now = clock.UtcNow;
            var cutoff = now.AddSeconds(-lockOptions.Value.DuplicateWindowSeconds);
            var priorLock = await FindRecentLockAsync(db, capture, cutoff, ct);
            if (priorLock is not null)
                return Result<GateAuthorization>.Ok(new(priorLock.Id));

            if (capture.CapturedAtUtc < cutoff ||
                !await subscribers.CanLockForVehicleAsync(capture.VehicleNumber, clock.ToBusinessTime(now), ct))
                return Fail("SubscriberLockNotEligible");
        }
        else if (command.Type != GateOperationType.Manual)
            return Fail("TicketNotFound");

        var softwareExit = command.Type == GateOperationType.Exit && !options.Value.EnableExitGateMqtt;
        if (!softwareExit && await FindControllerReservationAsync(db, numeric, ct) is { } reserved)
            return command.Type == GateOperationType.Manual
                ? CheckPrior(reserved, gate.Id, numeric, command.Type, command.HasRequestId, logger) : Fail("GateOperationPending");

        var operation = new GateOperation(command.OperationId,
            command.TicketId, gate.Id, command.UserId, numeric, command.Type, clock.UtcNow,
            isAutomaticVisitorEntry: hardware is not null,
            completionMode: softwareExit ? GateOperationCompletionMode.Software : GateOperationCompletionMode.Hardware);
        await db.Repository<GateOperation>().AddAsync(operation, ct);

        if (softwareExit)
        {
            // Acquire the ticket reservation under the unique index before archiving.
            // The transaction pipeline commits creation, completion and archive as one unit.
            await db.SaveChangesAsync(ct);
            operation.CompleteSoftwareExit(clock.UtcNow);
            var exit = await archives.CompleteAsync(operation, ct);
            return Result<GateAuthorization>.Ok(new(operation.Id, Created: true, Exit: exit));
        }

        hardware?.RecordGateOperation(command.OperationId);
        return Result<GateAuthorization>.Ok(new(command.OperationId, Created: true));
    }

    internal static async Task<GateOperation?> FindRecentLockAsync(IUnitOfWork db, LprCapture capture,
        DateTime cutoff, CancellationToken ct)
    {
        var attempts = await db.ListWithSpecAsync(new Matching<LprCapture>(x => x.GateId == capture.GateId &&
            x.ControllerId == capture.ControllerId && x.NormalizedVehicleNumber == capture.NormalizedVehicleNumber &&
            x.CapturedAtUtc >= cutoff), ct);
        var attemptIds = attempts.Select(x => x.Id).ToArray();
        return await db.FirstOrDefaultWithSpecAsync(new Matching<GateOperation>(x =>
            attemptIds.Contains(x.Id) && x.Type == GateOperationType.LockCardButton), ct);
    }

    internal static Task<GateOperation?> FindControllerReservationAsync(IUnitOfWork db, ushort controller, CancellationToken ct) =>
        db.FirstOrDefaultWithSpecAsync(new Matching<GateOperation>(x => x.ReservedControllerId == controller &&
            (x.Status == GateOperationStatus.Pending || x.Status == GateOperationStatus.Published)), ct);

    internal static Task<GateOperation?> FindCompletedAsync(IUnitOfWork db, Guid ticket, GateOperationType type, CancellationToken ct) =>
        db.FirstOrDefaultWithSpecAsync(new Matching<GateOperation>(x => x.TicketIdSnapshot == ticket &&
            x.Type == type && x.Status == GateOperationStatus.Completed), ct);

    internal static Result<GateAuthorization> CheckReplay(GateOperation prior, AuthorizeGateOperation request) =>
        prior.TicketIdSnapshot == request.TicketId && prior.GateId == request.GateId && prior.UserId == request.UserId &&
        prior.Type == request.Type && prior.IsAutomaticVisitorEntry == request.HardwareEntry &&
        (!request.Controller.HasValue || prior.ControllerId == request.Controller.Value)
            ? Result<GateAuthorization>.Ok(new(prior.Id)) : Fail("GateRequestIdConflict");

    internal static async Task<(ParkingGate Gate, ushort Controller)?> ResolveGateAsync(
        IUnitOfWork db, Guid? requestedGateId, ushort? controller, CancellationToken ct)
    {
        var gate = requestedGateId is Guid gateId
            ? await db.Repository<ParkingGate>().GetByIdAsync(gateId, ct)
            : null;
        if (requestedGateId.HasValue)
        {
            if (gate is null || ParseController(gate.ControlIp) is not ushort mapped ||
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

    internal static ushort? ParseController(string? value) =>
        ushort.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id != 0 &&
        value == id.ToString(CultureInfo.InvariantCulture) ? id : null;

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
