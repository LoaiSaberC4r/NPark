using NPark.Application.Feature.LprSignalRManagement.Command.VehicleDetected;

namespace NPark.Application.Abstraction
{
    public interface ILprSignalRNotifier
    {
        Task NotifyVehicleDetectedAsync(
            LprVehicleDetectedCommandResponse payload,
            CancellationToken cancellationToken);
    }
}