namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.CheckMembershipByCardNumber
{
    public sealed record CheckMembershipByCardNumberQueryResponse
    {
        public bool IsMember { get; init; }
    }
}