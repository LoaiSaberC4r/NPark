using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.UserManagement.Query.GetUsersByRoleId
{
    internal sealed class GetUsersByRoleIdSpec
        : Specification<User, UserByRoleResponse>
    {
        public GetUsersByRoleIdSpec(GetUsersByRoleIdQuery request)
        {
            AddCriteria(x => x.RoleId == request.RoleId);

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var searchText = request.SearchText.Trim();

                AddCriteria(x =>
                    x.Name.Contains(searchText) ||
                    x.Username.Contains(searchText) ||
                    x.Email.Contains(searchText) ||
                    x.PhoneNumber.Contains(searchText) ||
                    x.NationalId.Contains(searchText));
            }

            AddOrderBy(x => x.Name);

            EnableTotalCount();
            ApplyPaging(request.PageNumber, request.PageSize);

            Select(x => new UserByRoleResponse
            {
                UserId = x.Id,

                Name = x.Name,
                Username = x.Username,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                NationalId = x.NationalId,

                RoleId = x.RoleId,
                RoleNameEn = x.Role.NameEn,
                RoleNameAr = x.Role.NameAr,

                ShiftStartTime = x.ShiftStartTime,
                ShiftEndTime = x.ShiftEndTime
            });
        }
    }
}