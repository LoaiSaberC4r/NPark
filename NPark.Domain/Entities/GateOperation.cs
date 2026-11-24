using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;

namespace NPark.Domain.Entities;

public sealed class GateOperation : Entity<Guid>
{
    private GateOperation() { }

    public GateOperation(Guid id, Guid? ticketId, Guid gateId, Guid? userId,
        ushort controllerId, GateOperationType type, DateTime utc)
    {
        Id = id;
        TicketIdSnapshot = ticketId;
        ReservedTicketId = ticketId;
        GateId = gateId;
        UserId = userId;
        ControllerId = controllerId;
        ReservedControllerId = controllerId;
        Type = type;
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

    public void BeginDispatch(DateTime utc, bool retry)
    {
        DispatchStartedAtUtc = utc;
        PublishedAtUtc = null;
        Status = GateOperationStatus.Pending;
        ErrorCode = null;
        if (retry)
            RetryCount++;
    }

    public void MarkPublished(DateTime utc)
    {
        PublishedAtUtc ??= utc;
        if (Status == GateOperationStatus.Pending)
            Status = GateOperationStatus.Published;
    }

    public void MarkUncertain(bool timeout)
    {
        if (Status is not (GateOperationStatus.Pending or GateOperationStatus.Published))
            return;
        Status = timeout ? GateOperationStatus.TimedOut : GateOperationStatus.Unknown;
        ErrorCode = timeout ? "GateAckTimeout" : "GateDeliveryUnknown";
    }

    public bool Acknowledge(byte code, DateTime utc)
    {
        if (!DispatchStartedAtUtc.HasValue || AckStatusCode.HasValue || ReservedControllerId is null)
            return false;
        AckStatusCode = code;
        AcknowledgedAtUtc = utc;
        Status = code == 0 ? GateOperationStatus.Acknowledged : GateOperationStatus.Failed;
        ErrorCode = code switch { 1 => "GateExecutionFailed", 2 => "GateBusy", 3 => "GateRejected", _ => null };
        if (code != 0)
            Release();
        return true;
    }

    public void Complete(DateTime utc)
    {
        if (Status != GateOperationStatus.Acknowledged || AckStatusCode != 0)
            throw new InvalidOperationException("A successful hardware ACK is required.");
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
        Status = GateOperationStatus.Failed;
        ErrorCode = "GateNonExecutionReconciled";
        Release();
    }

    private void Release()
    {
        ReservedTicketId = null;
        ReservedControllerId = null;
    }
}
