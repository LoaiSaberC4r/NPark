using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Abstraction;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.LprSignalRManagement.Command.VehicleDetected
{
    internal sealed class LprVehicleDetectedCommandHandler
      : ICommandHandler<LprVehicleDetectedCommand, LprVehicleDetectedCommandResponse>
    {
        private readonly IGenericRepository<ParkingGate> _parkingGateRepository;
        private readonly ILprSignalRNotifier _notifier;

        public LprVehicleDetectedCommandHandler(
            IGenericRepository<ParkingGate> parkingGateRepository,
            ILprSignalRNotifier notifier)
        {
            _parkingGateRepository = parkingGateRepository;
            _notifier = notifier;
        }

        public async Task<Result<LprVehicleDetectedCommandResponse>> Handle(
            LprVehicleDetectedCommand request,
            CancellationToken cancellationToken)
        {
            var lprIp = request.LprIp.Trim();
            var vehicleNumber = request.VehicleNumber.Trim();

            var gate = await _parkingGateRepository.GetByPropertyAsync(
                x => x.LprIp != null && x.LprIp == lprIp);

            if (gate is null)
            {
                return Result<LprVehicleDetectedCommandResponse>.Fail(
                    new Error(
                        Code: "LPR_IP_NOT_FOUND",
                        Message: ErrorMessage.LprIpNotFound,
                        Type: ErrorType.NotFound));
            }

            var response = new LprVehicleDetectedCommandResponse
            {
                GateId = gate.Id,
                VehicleNumber = vehicleNumber
            };

            await _notifier.NotifyVehicleDetectedAsync(response, cancellationToken);

            return Result<LprVehicleDetectedCommandResponse>.Ok(response);
        }
    }
}