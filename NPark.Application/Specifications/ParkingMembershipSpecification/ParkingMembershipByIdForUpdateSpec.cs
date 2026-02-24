using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.ParkingMembershipSpecification
{
    internal sealed class ParkingMembershipByIdForUpdateSpec : Specification<ParkingMemberships>
    {
        public ParkingMembershipByIdForUpdateSpec(Guid id)
        {
            AddCriteria(x => x.Id == id);
            Include(x => x.PricingScheme);
            Include(x => x.Attachments);
        }
    }
}