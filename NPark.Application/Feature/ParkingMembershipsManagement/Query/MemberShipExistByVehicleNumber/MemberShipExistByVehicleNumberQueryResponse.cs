namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.MemberShipExistByVehicleNumber
{
    public sealed record class MemberShipExistByVehicleNumberQueryResponse
    {
        public bool IsExist { get; init; }
    }
}