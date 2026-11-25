namespace NPark.Domain.Entities;

public sealed class LprCapture
{
    private LprCapture() { }

    public LprCapture(Guid gateId, int? controllerId, string lprIp, string vehicleNumber, DateTime capturedAtUtc)
    {
        Id = Guid.NewGuid();
        GateId = gateId;
        ControllerId = controllerId;
        LprIp = lprIp;
        VehicleNumber = vehicleNumber;
        NormalizedVehicleNumber = vehicleNumber.Trim().Replace(" ", "").ToUpperInvariant();
        CapturedAtUtc = capturedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid GateId { get; private set; }
    public int? ControllerId { get; private set; }
    public string LprIp { get; private set; } = null!;
    public string VehicleNumber { get; private set; } = null!;
    public string NormalizedVehicleNumber { get; private set; } = null!;
    public DateTime CapturedAtUtc { get; private set; }
    public Guid? TicketId { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public void Claim(Guid ticketId, DateTime processedAtUtc)
    {
        if (TicketId.HasValue)
            throw new InvalidOperationException("The LPR capture is already associated with a ticket.");

        TicketId = ticketId;
        ProcessedAtUtc = processedAtUtc;
    }
}
