using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.LprSignalRManagement.Command.VehicleDetected
{
    public sealed record LprVehicleDetectedCommand : ICommand<LprVehicleDetectedCommandResponse>
    {
        public string LprIp { get; init; } = string.Empty;
        public string VehicleNumber { get; init; } = string.Empty;
    }
}