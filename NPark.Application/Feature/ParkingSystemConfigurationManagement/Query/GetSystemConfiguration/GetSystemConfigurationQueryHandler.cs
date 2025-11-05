using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Specifications.ParkingSystemConfigurationSpec;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.ParkingSystemConfigurationManagement.Query.GetSystemConfiguration
{
    public class GetSystemConfigurationQueryHandler : IQueryHandler<GetSystemConfigurationQuery, GetSystemConfigurationQueryResponse>
    {
        private readonly IGenericRepository<ParkingSystemConfiguration> _parkingSystemConfigurationRepository;

        public GetSystemConfigurationQueryHandler(IGenericRepository<ParkingSystemConfiguration> parkingSystemConfigurationRepository)
        {
            _parkingSystemConfigurationRepository = parkingSystemConfigurationRepository ?? throw new ArgumentNullException(nameof(parkingSystemConfigurationRepository));
        }

        public async Task<Result<GetSystemConfigurationQueryResponse>> Handle(GetSystemConfigurationQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetParkingSystemConfigurationSpecification();
            var entity = await _parkingSystemConfigurationRepository.FirstOrDefaultWithSpecAsync(spec, cancellationToken);
            return Result<GetSystemConfigurationQueryResponse>.Ok(entity ?? new GetSystemConfigurationQueryResponse());
        }
    }
}