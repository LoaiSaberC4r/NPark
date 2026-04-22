using BuildingBlock.Domain.Specification;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.MemberShipExistByVehicleNumber
{
    internal sealed class MemberShipExistByVehicleNumberSpec : Specification<Domain.Entities.ParkingMemberships>
    {
        public MemberShipExistByVehicleNumberSpec(string vehicleNumber)
        {
            AddCriteria(t => t.VehicleNumber.Trim() == vehicleNumber.Trim());
        }
    }
}