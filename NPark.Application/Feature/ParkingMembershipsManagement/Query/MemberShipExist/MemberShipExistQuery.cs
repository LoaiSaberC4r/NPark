using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.MemberShipExist
{
    public sealed record class MemberShipExistQuery
         : IQuery<MemberShipExistQueryResponse>
    {
        /// <summary>
        /// Can be VehicleNumber or CardNumber.
        /// Backend checks VehicleNumber first, then CardNumber.
        /// </summary>
        public string SearchValue { get; init; } = string.Empty;
    }
    public sealed record class MemberShipExistQueryResponse
    {
        public bool IsExist { get; init; }
    }
}