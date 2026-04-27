using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.CheckMembershipByCardNumber
{
    internal sealed class CheckMembershipByCardNumberSpec
        : Specification<ParkingMemberships, CheckMembershipByCardNumberQueryResponse>
    {
        public CheckMembershipByCardNumberSpec(string cardNumber)
        {
            AddCriteria(x => x.CardNumber == cardNumber);

            Select(x => new CheckMembershipByCardNumberQueryResponse
            {
                IsMember = true
            });
        }
    }
}