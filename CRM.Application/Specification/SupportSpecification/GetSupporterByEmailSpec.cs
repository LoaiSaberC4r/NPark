using BuildingBlock.Domain.Specification;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.SupportSpecification
{
    internal sealed class GetSupporterByEmailSpec : Specification<Supporter>
    {
        public GetSupporterByEmailSpec(string email)
        {
            AddCriteria(x => x.Email == email);
            UseNoTracking();
        }
    }
}