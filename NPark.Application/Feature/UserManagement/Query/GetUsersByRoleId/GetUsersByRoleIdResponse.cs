using BuildingBlock.Domain.SharedDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.UserManagement.Query.GetUsersByRoleId
{
    public sealed record GetUsersByRoleIdResponse
    {
        public Guid RoleId { get; init; }
        public string RoleNameEn { get; init; } = string.Empty;
        public string RoleNameAr { get; init; } = string.Empty;

        public Pagination<UserByRoleResponse> Users { get; init; } = null!;
    }
}