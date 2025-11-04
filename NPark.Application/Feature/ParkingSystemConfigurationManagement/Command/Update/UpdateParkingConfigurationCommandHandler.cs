using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.ParkingSystemConfigurationManagement.Command.Update
{
    public class UpdateParkingConfigurationCommandHandler : ICommandHandler<UpdateParkingConfigurationCommand>
    {
        private readonly IGenericRepository<ParkingSystemConfiguration> _parkingSystemConfigurationRepository;

        public UpdateParkingConfigurationCommandHandler(IGenericRepository<ParkingSystemConfiguration> parkingSystemConfigurationRepository)
        {
            _parkingSystemConfigurationRepository = parkingSystemConfigurationRepository ?? throw new ArgumentNullException(nameof(parkingSystemConfigurationRepository));
        }

        public async Task<Result> Handle(UpdateParkingConfigurationCommand request, CancellationToken cancellationToken)
        {
            var entity = await _parkingSystemConfigurationRepository.GetByIdAsync(1, cancellationToken);
            if (entity is null)
            {
                entity = ParkingSystemConfiguration.Create(
                       request.EntryGatesCount,
                       request.ExitGatesCount,
                       request.AllowedParkingSlots,
                       request.PriceType,
                       request.VehiclePassengerData,
                       request.PrintType,
                       request.DateTimeFlag,
                       request.TicketIdFlag,
                       request.FeesFlag,
                       request.PricingSchemaId == Guid.Empty ? null : request.PricingSchemaId
                   );
                await _parkingSystemConfigurationRepository.AddAsync(entity, cancellationToken);
                await _parkingSystemConfigurationRepository.SaveChangesAsync(cancellationToken);
                return Result.Ok();
            }
            else
            {
                entity.Update(
                       entity,
                       request.EntryGatesCount,
                       request.ExitGatesCount,
                       request.AllowedParkingSlots,
                       request.PriceType,
                       request.VehiclePassengerData,
                       request.PrintType,
                       request.DateTimeFlag,
                       request.TicketIdFlag,
                       request.FeesFlag,
                       request.PricingSchemaId == Guid.Empty ? null : request.PricingSchemaId
                   );
                await _parkingSystemConfigurationRepository.SaveChangesAsync(cancellationToken);
                return Result.Ok();
            }
        }
    }
}