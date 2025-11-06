using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.ParkingSystemConfigurationSpec
{
    public class GetParkingSystemConfigurationForUpdateSpecification : Specification<ParkingSystemConfiguration>
    {
        public GetParkingSystemConfigurationForUpdateSpecification()
        {
            AddCriteria(x => x.Id == 1);
            Include(x => x.ParkingGates);
        }
    }
}