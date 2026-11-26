using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;

namespace NPark.Domain.Entities;

public sealed class GateOperation : Entity<Guid>
{
    private GateOperation() { }

    public GateOperation(Guid id, Guid? ticketId, Guid gateId, Guid? userId,
        ushort controllerId, GateOperationType type, DateTime utc, bool isAutomaticVisitorEntry = false,
        GateOperationCompletionMode completionMode = GateOperationCompletionMode.Hardware)
    {
        if (completionMode != GateOperationCompletionMode.Hardware &&
            (completionMode != GateOperationCompletionMode.Software || type != GateOperationType.Exit ||
             ticketId is null || ticketId == Guid.Empty || isAutomaticVisitorEntry))
            throw new InvalidOperationException("Software confirmation requires a ticket exit operation.");

        Id = id;
        TicketIdSnapshot = ticketId;
        ReservedTicketId = ticketId;
        GateId = gateId;
        UserId = userId;
        ControllerId = controllerId;
        ReservedControllerId = completionMode == GateOperationCompletionMode.Hardware ? controllerId : null;
        Type = type;
        CompletionMode = completionMode;
        IsAutomaticVisitorEntry = isAutomaticVisitorEntry;
        Status = GateOperationStatus.Pending;
        CreatedAtUtc = utc;
    }

    public long CommandId { get; private set; }
    public Guid? TicketIdSnapshot { get; private set; }
    public Guid? ReservedTicketId { get; private set; }
    public int? ReservedControllerId { get; private set; }
    public Guid GateId { get; private set; }
    public Guid? UserId { get; private set; }
    public int ControllerId { get; private set; }
    public GateOperationType Type { get; private set; }
    public GateOperationCompletionMode CompletionMode { get; private set; }
    public bool IsAutomaticVisitorEntry { get; private set; }
    public GateOperationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DispatchStartedAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int RetryCount { get; private set; }
    public byte? AckStatusCode { get; private set; }
    public Guid? ReconciledBy { get; private set; }
    public string? ReconciliationEvidence { get; private set; }
    public DateTime? ReconciledAtUtc { get; private set; }
    public string? ErrorCode { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void BeginDispatch(DateTime utc)
    {
        if (CompletionMode != GateOperationCompletionMode.Hardware ||
            Status != GateOperationStatus.Pending || DispatchStartedAtUtc.HasValue)
            throw new InvalidOperationException("Only an unclaimed pending operation can be dispatched.");
        DispatchStartedAtUtc = utc;
    }

    public void MarkPublished(DateTime utc)
    {
        if (Status != GateOperationStatus.Pending || !DispatchStartedAtUtc.HasValue)
            return;
        PublishedAtUtc = utc;
        Status = GateOperationStatus.Published;
    }

    public void MarkDispatchExpired()
    {
        if (CompletionMode != GateOperationCompletionMode.Hardware ||
            Status != GateOperationStatus.Pending || DispatchStartedAtUtc.HasValue)
            return;
        Status = GateOperationStatus.TimedOut;
        ErrorCode = "GateDispatchTimeout";
        Release();
    }

    public void CancelUndispatchedHardwareExit()
    {
        if (CompletionMode != GateOperationCompletionMode.Hardware || Type != GateOperationType.Exit ||
            Status != GateOperationStatus.Pending || DispatchStartedAtUtc.HasValue)
            return;

        Status = GateOperationStatus.Failed;
        ErrorCode = "GateExitMqttDisabled";
        Release();
    }

    public void MarkUncertain(bool timeout)
    {
        if (CompletionMode != GateOperationCompletionMode.Hardware ||
            Status is not (GateOperationStatus.Pending or GateOperationStatus.Published))
            return;
        Status = timeout ? GateOperationStatus.TimedOut : GateOperationStatus.Unknown;
        ErrorCode = timeout ? "GateAckTimeout" : "GateDeliveryUnknown";
        Release();
    }

    public bool Acknowledge(byte code, DateTime utc)
    {
        if (CompletionMode != GateOperationCompletionMode.Hardware ||
            Status is not (GateOperationStatus.Pending or GateOperationStatus.Published) ||
            !DispatchStartedAtUtc.HasValue || AckStatusCode.HasValue || ReservedControllerId is null || code > 3)
            return false;
        AckStatusCode = code;
        AcknowledgedAtUtc = utc;
        Status = code == 0 ? GateOperationStatus.Acknowledged : GateOperationStatus.Failed;
        ErrorCode = code switch { 1 => "GateExecutionFailed", 2 => "GateBusy", 3 => "GateRejected", _ => null };
        ReleaseController();
        if (code != 0)
            ReleaseTicket();
        return true;
    }

    public void Complete(DateTime utc)
    {
        if (CompletionMode != GateOperationCompletionMode.Hardware ||
            Status != GateOperationStatus.Acknowledged || AckStatusCode != 0)
            throw new InvalidOperationException("A successful hardware ACK is required.");
        Finish(utc);
    }

    public void CompleteSoftwareExit(DateTime utc)
    {
        if (CompletionMode != GateOperationCompletionMode.Software || Type != GateOperationType.Exit ||
            Status != GateOperationStatus.Pending || TicketIdSnapshot is null || TicketIdSnapshot == Guid.Empty ||
            ReservedTicketId != TicketIdSnapshot || ReservedControllerId.HasValue || DispatchStartedAtUtc.HasValue ||
            PublishedAtUtc.HasValue || AcknowledgedAtUtc.HasValue || AckStatusCode.HasValue)
            throw new InvalidOperationException("Only an authorized pending software exit can be confirmed.");

        Finish(utc);
    }

    private void Finish(DateTime utc)
    {
        Status = GateOperationStatus.Completed;
        CompletedAtUtc = utc;
        Release();
    }

    public void ReconcileNonExecution(Guid actor, string evidence, DateTime utc)
    {
        if (Status is not (GateOperationStatus.TimedOut or GateOperationStatus.Unknown))
            throw new InvalidOperationException("Only uncertain execution can be reconciled.");
        ReconciledBy = actor;
        ReconciliationEvidence = evidence;
        ReconciledAtUtc = utc;
    }

    private void ReleaseController() => ReservedControllerId = null;

    private void ReleaseTicket() => ReservedTicketId = null;

    private void Release()
    {
        ReleaseController();
        ReleaseTicket();
    }
}
