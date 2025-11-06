using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.ParkingGateSpec
{
    public sealed class GetParkingGateBySystemConfiguration : Specification<ParkingGate>
    {
        public GetParkingGateBySystemConfiguration(int Id)
        {
            AddCriteria(x => x.ParkingSystemConfigurationId == Id);
        }
    }
}