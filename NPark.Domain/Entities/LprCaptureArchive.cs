namespace NPark.Domain.Entities;

public sealed record LprCaptureArchive(Guid Id, Guid GateId, int? ControllerId, string LprIp,
    string VehicleNumber, string NormalizedVehicleNumber, DateTime CapturedAtUtc, Guid? TicketId,
    DateTime? ProcessedAtUtc, DateTime ArchivedAtUtc)
{
    public static LprCaptureArchive FromCapture(LprCapture capture, DateTime archivedAtUtc) =>
        new(capture.Id, capture.GateId, capture.ControllerId, capture.LprIp, capture.VehicleNumber,
            capture.NormalizedVehicleNumber, capture.CapturedAtUtc, capture.TicketId, capture.ProcessedAtUtc, archivedAtUtc);
}
