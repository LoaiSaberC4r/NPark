using System.Globalization;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
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

public sealed class GateOperationService(IUnitOfWork db, IServiceScopeFactory scopes, IOptions<GateOperationOptions> options)
{
    public async Task<Result<GateOperationResponse>> AuthorizeAsync(
        Guid? ticketId, Guid? gateId, ushort? controller, Guid? userId, GateOperationType type, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await using var phase = scopes.CreateAsyncScope();
        var sender = phase.ServiceProvider.GetRequiredService<ISender>();
        var operationDb = phase.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var result = await sender.Send(new AuthorizeGateOperation(id, ticketId, gateId, controller, userId, type), ct);
        if (result.IsFailure)
        {
            // A concurrent request can win the reservation after this request reads eligibility.
            var existing = ticketId.HasValue
                ? await TicketExitEligibilityService.FindReservationAsync(operationDb, ticketId.Value, ct)
                : null;
            if (existing is not null && existing.Type == type &&
                (gateId is null || existing.GateId == gateId) &&
                (controller is null || existing.ControllerId == controller))
                return Result<GateOperationResponse>.Ok(ToResponse(existing));
            return Result<GateOperationResponse>.Fail(result.Errors);
        }
        var operation = await operationDb.Repository<GateOperation>().GetByIdAsync(result.Value, ct);
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
    ParkingBusinessClock clock) : ICommandHandler<AuthorizeGateOperation, Guid>
{
    public async Task<Result<Guid>> Handle(AuthorizeGateOperation command, CancellationToken ct)
    {
        ParkingGate? gate;
        if (command.GateId is Guid gateId)
            gate = await db.Repository<ParkingGate>().GetByIdAsync(gateId, ct);
        else if (command.Controller is ushort controller)
        {
            var controllerText = controller.ToString(CultureInfo.InvariantCulture);
            var matches = await db.ListWithSpecAsync(new Matching<ParkingGate>(g => g.ControlIp == controllerText, 2), ct);
            gate = matches.Count == 1 ? matches[0] : null;
        }
        else
            gate = null;

        if (gate is null || !ushort.TryParse(gate.ControlIp, NumberStyles.None, CultureInfo.InvariantCulture, out var numeric)
            || numeric == 0 || command.Controller.HasValue && command.Controller != numeric)
            return Fail("InvalidExitGate");

        var registered = numeric.ToString(CultureInfo.InvariantCulture);
        var mappings = await db.ListWithSpecAsync(new Matching<ParkingGate>(x => x.ControlIp == registered, 2), ct);
        if (mappings.Count != 1 || mappings[0].Id != gate.Id)
            return Fail("InvalidExitGate");

        if (command.TicketId is Guid ticketId)
        {
            var prior = await db.FirstOrDefaultWithSpecAsync(new Matching<GateOperation>(x =>
                x.TicketIdSnapshot == ticketId && (x.ReservedTicketId != null ||
                    x.Type == command.Type && x.Status == GateOperationStatus.Completed)), ct);
            if (prior is not null)
                return prior.GateId == gate.Id && prior.Type == command.Type
                    ? Result<Guid>.Ok(prior.Id) : Fail("GateOperationPending");

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
            return reserved.Type == GateOperationType.Manual && command.Type == GateOperationType.Manual
                && reserved.GateId == gate.Id ? Result<Guid>.Ok(reserved.Id) : Fail("GateOperationPending");

        await db.Repository<GateOperation>().AddAsync(new GateOperation(command.OperationId,
            command.TicketId, gate.Id, command.UserId, numeric, command.Type, clock.UtcNow), ct);
        return Result<Guid>.Ok(command.OperationId);
    }

    private static Result<Guid> Fail(string reason) => Result<Guid>.Fail(TicketExitEligibilityService.AsError(reason));
}
