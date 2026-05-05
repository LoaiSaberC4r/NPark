using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.UserManagement.Query.GetUsersByRoleId
{
    internal sealed class GetRoleForUsersGroupSpec
        : Specification<Role, RoleForUsersGroupResponse>
    {
        public GetRoleForUsersGroupSpec(Guid roleId)
        {
            AddCriteria(x => x.Id == roleId);

            Select(x => new RoleForUsersGroupResponse
            {
                RoleId = x.Id,
                RoleNameEn = x.NameEn,
                RoleNameAr = x.NameAr
            });
        }
    }
}