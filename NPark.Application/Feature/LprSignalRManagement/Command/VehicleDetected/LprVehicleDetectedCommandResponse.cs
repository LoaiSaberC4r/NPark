namespace NPark.Application.Feature.LprSignalRManagement.Command.VehicleDetected
{
    public sealed record LprVehicleDetectedCommandResponse
    {
        public Guid GateId { get; init; }
        public string VehicleNumber { get; init; } = string.Empty;
    }
}