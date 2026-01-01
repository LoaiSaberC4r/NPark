using BuildingBlock.Domain.Specification;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.AuthSpecification
{
    internal sealed class LoginSpec : Specification<SuperAdmin>
    {
        public LoginSpec(string email)
        {
            AddCriteria(x => x.Email == email);
            UseNoTracking();
        }
    }
}