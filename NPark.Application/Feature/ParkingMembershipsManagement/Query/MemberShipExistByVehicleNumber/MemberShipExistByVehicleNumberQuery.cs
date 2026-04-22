using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.MemberShipExistByVehicleNumber
{
    public sealed record class MemberShipExistByVehicleNumberQuery
        : IQuery<MemberShipExistByVehicleNumberQueryResponse>
    {
        public string VehicleNumber { get; init; } = string.Empty;
    }
}