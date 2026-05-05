using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.SharedDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.UserManagement.Query.GetUsersByRoleId
{
    public sealed class GetUsersByRoleIdQuery : SearchParameters, IQuery<GetUsersByRoleIdResponse>
    {
        public Guid RoleId { get; set; }
    }
}