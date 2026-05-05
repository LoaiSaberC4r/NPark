using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.UserManagement.Query.GetUsersByRoleId
{
    public sealed record UserByRoleResponse
    {
        public Guid UserId { get; init; }

        public string Name { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string NationalId { get; init; } = string.Empty;

        public Guid RoleId { get; init; }
        public string RoleNameEn { get; init; } = string.Empty;
        public string RoleNameAr { get; init; } = string.Empty;

        public TimeSpan? ShiftStartTime { get; init; }
        public TimeSpan? ShiftEndTime { get; init; }
    }
}