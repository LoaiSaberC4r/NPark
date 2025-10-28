using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.UserSpecification
{
    public class GetUserByUserName : Specification<User>
    {
        public GetUserByUserName(string userName)
        {
            AddCriteria(q => q.Username == userName);
        }
    }
}