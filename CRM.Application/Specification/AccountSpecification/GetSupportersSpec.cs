using BuildingBlock.Domain.Specification;
using CRM.Application.Feature.AccountManagement.Query.GetSupporters;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.AccountSpecification
{
    internal sealed class GetSupportersSpec : Specification<SuperAdmin, GetSupportersQueryResponse>
    {
        public GetSupportersSpec(Guid roleId)
        {
            AddCriteria(x => x.RoleId == roleId);
            Select(x => new GetSupportersQueryResponse { Id = x.Id, Email = x.Email });
            UseNoTracking();
        }
    }
}