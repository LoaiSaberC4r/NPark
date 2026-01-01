using BuildingBlock.Domain.Specification;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.AuthSpecification
{
    internal sealed class LoginWithEmailSpec : Specification<SuperAdmin>
    {
        public LoginWithEmailSpec(Guid userId)
        {
            AddCriteria(x => x.Id == userId);
        }
    }
}