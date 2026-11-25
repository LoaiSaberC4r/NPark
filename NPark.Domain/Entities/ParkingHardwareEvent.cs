namespace NPark.Domain.Entities;

public enum ParkingEventProcessingStatus
{
    Received,
    TicketCreated,
    GateOperationCreated,
    Completed,
    Rejected,
    Failed,
    Ignored
}

public sealed class ParkingHardwareEvent
{
    private ParkingHardwareEvent() { }

    public ParkingHardwareEvent(int controllerId, byte[] eventId, byte schema, byte lane, byte outcome,
        byte flags, string? cardNumber, uint hardwareTimestamp, DateTime? hardwareOccurredAtUtc,
        DateTime receivedAtUtc, byte[] rawPayload)
    {
        Id = Guid.NewGuid();
        ControllerId = controllerId;
        EventId = eventId;
        Schema = schema;
        Lane = lane;
        Outcome = outcome;
        Flags = flags;
        CardNumber = cardNumber;
        RawHardwareTimestamp = hardwareTimestamp;
        HardwareOccurredAtUtc = hardwareOccurredAtUtc;
        ReceivedAtUtc = receivedAtUtc;
        RawPayload = rawPayload;
    }

    // The event's primary key is also the stable GateOperation request ID.
    public Guid Id { get; private set; }
    public int ControllerId { get; private set; }
    public Guid? GateId { get; private set; }
    public byte[] EventId { get; private set; } = null!;
    public byte Schema { get; private set; }
    public byte Lane { get; private set; }
    public byte Outcome { get; private set; }
    public byte Flags { get; private set; }
    public string? CardNumber { get; private set; }
    public long RawHardwareTimestamp { get; private set; }
    public DateTime? HardwareOccurredAtUtc { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public byte[] RawPayload { get; private set; } = null!;
    public DateTime? StorageAckSentAtUtc { get; private set; }
    public Guid? LprCaptureId { get; private set; }
    public Guid? TicketId { get; private set; }
    public Guid? GateOperationId { get; private set; }
    public ParkingEventProcessingStatus ProcessingStatus { get; private set; }
    public string? FailureReasonCode { get; private set; }
    public int ProcessingAttempts { get; private set; }
    public DateTime? RetryAfterUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public void ResolveGate(Guid gateId) => GateId = gateId;

    public void RecordTicket(Guid ticketId, Guid? lprCaptureId)
    {
        TicketId = ticketId;
        LprCaptureId = lprCaptureId;
        ProcessingStatus = ParkingEventProcessingStatus.TicketCreated;
    }

    public void RecordGateOperation(Guid operationId)
    {
        GateOperationId = operationId;
        ProcessingStatus = ParkingEventProcessingStatus.GateOperationCreated;
    }

    public void Finish(ParkingEventProcessingStatus status, string? reason, DateTime now)
    {
        ProcessingStatus = status;
        FailureReasonCode = reason;
        ProcessedAtUtc = now;
    }
}
