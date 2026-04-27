using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.CheckMembershipByCardNumber
{
    public sealed record CheckMembershipByCardNumberQuery(string CardNumber)
        : IQuery<CheckMembershipByCardNumberQueryResponse>;
}