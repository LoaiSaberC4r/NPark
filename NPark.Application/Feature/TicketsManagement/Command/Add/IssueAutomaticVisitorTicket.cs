using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.Specification;
using Microsoft.Extensions.Options;
using NPark.Application.Options;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Application.Feature.TicketsManagement.Command.Add;

public sealed record IssueAutomaticVisitorTicket(Guid EventRecordId) : ITransactionalCommand<AutomaticVisitorTicketResult>;
public sealed record AutomaticVisitorTicketResult(Ticket? Ticket = null, ParkingGate? Gate = null,
    Guid? LprCaptureId = null, string? ReasonCode = null);

internal sealed class IssueAutomaticVisitorTicketHandler(IUnitOfWork db, TicketIssuanceService issuance,
    ParkingBusinessClock clock, IOptions<AutomaticVisitorEntryOptions> entryOptions, IOptions<LprCaptureOptions> lprOptions)
    : ICommandHandler<IssueAutomaticVisitorTicket, AutomaticVisitorTicketResult>
{
    public async Task<Result<AutomaticVisitorTicketResult>> Handle(IssueAutomaticVisitorTicket request, CancellationToken ct)
    {
        var hardware = await db.Repository<ParkingHardwareEvent>().GetByIdAsync(request.EventRecordId, ct);
        if (hardware is null || hardware.ProcessingStatus != ParkingEventProcessingStatus.Received ||
            hardware.StorageAckSentAtUtc is null || !entryOptions.Value.Enabled)
            return Result<AutomaticVisitorTicketResult>.Ok(new());

        var now = clock.UtcNow;
        Result<AutomaticVisitorTicketResult> Finish(ParkingEventProcessingStatus status, string reason)
        {
            hardware.Finish(status, reason, now);
            return Result<AutomaticVisitorTicketResult>.Ok(new(ReasonCode: reason));
        }

        if (hardware.Schema != 1 || hardware.Lane != 1 || hardware.Outcome != 1)
            return Finish(ParkingEventProcessingStatus.Ignored, "ParkingEventNotVisitorPickup");
        if (hardware.CardNumber is not { Length: 8 } card)
            return Finish(ParkingEventProcessingStatus.Rejected, "ParkingEventCardUidRequired");
        if (!entryOptions.Value.Supports(hardware.ControllerId))
            return Finish(ParkingEventProcessingStatus.Rejected, "UnsupportedVisitorController");

        var mapping = await AuthorizeGateOperationHandler.ResolveGateAsync(db, null, (ushort)hardware.ControllerId, ct);
        if (mapping is not { Gate.GateType: GateType.Entrance })
            return Finish(ParkingEventProcessingStatus.Rejected, "EntranceControllerNotMapped");

        var gate = mapping.Value.Gate;
        hardware.ResolveGate(gate.Id);
        if (gate.HasScanner == true)
            return Finish(ParkingEventProcessingStatus.Rejected, "ScannerPhotoRequired");

        var configuration = await db.Repository<ParkingSystemConfiguration>().GetByIdAsync(1, ct);
        if (configuration?.PriceType != PriceType.Exit)
            return Finish(ParkingEventProcessingStatus.Rejected, "AutomaticVisitorEntryRequiresExitPricing");

        if (entryOptions.Value.SystemUserId is not Guid userId ||
            !await db.AnyWithSpecAsync(new Matching<User>(x => x.Id == userId), ct))
            return Finish(ParkingEventProcessingStatus.Rejected, "AutomaticVisitorSystemActorUnavailable");

        if (await db.AnyWithSpecAsync(new Matching<Ticket>(x => x.EndDate == null && x.CardNumber == card), ct))
            return Finish(ParkingEventProcessingStatus.Rejected, "VisitorCardAlreadyInUse");

        LprCapture? capture = null;
        var lpr = lprOptions.Value;
        if (gate.HasLpr && lpr.Enabled)
        {
            var cutoff = now.AddDays(-lpr.RetentionDays);
            if (lpr.MaxMatchingAgeSeconds is int age)
            {
                var maxAgeCutoff = hardware.ReceivedAtUtc.AddSeconds(-age);
                if (maxAgeCutoff > cutoff)
                    cutoff = maxAgeCutoff;
            }
            var latest = await db.FirstOrDefaultWithSpecAsync(new LatestCapture(gate.Id, hardware.ControllerId,
                hardware.ReceivedAtUtc, cutoff), ct);
            if (latest?.TicketId is null)
                capture = latest;
        }

        var ticket = issuance.CreateVisitor(gate.Id, userId, PriceType.Exit, 0m, hardware.ReceivedAtUtc,
            capture?.VehicleNumber.Trim());
        ticket.AssignVisitorCard(card);
        await db.Repository<Ticket>().AddAsync(ticket, ct);
        capture?.Claim(ticket.Id, now);
        hardware.RecordTicket(ticket.Id, capture?.Id);
        return Result<AutomaticVisitorTicketResult>.Ok(new(ticket, gate, capture?.Id));
    }

    private sealed class LatestCapture : Specification<LprCapture>
    {
        public LatestCapture(Guid gateId, int controllerId, DateTime receipt, DateTime cutoff)
        {
            AddCriteria(x => x.ControllerId == controllerId && x.GateId == gateId &&
                x.CapturedAtUtc >= cutoff && x.CapturedAtUtc <= receipt);
            AddOrderByDescending(x => x.CapturedAtUtc);
            AddOrderByDescending(x => x.Id);
        }
    }
}
