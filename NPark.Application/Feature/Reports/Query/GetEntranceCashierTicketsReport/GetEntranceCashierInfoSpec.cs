using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetEntranceCashierTicketsReport
{
    internal sealed class GetEntranceCashierInfoSpec
         : Specification<User, EntranceCashierInfoResponse>
    {
        public GetEntranceCashierInfoSpec(Guid userId)
        {
            AddCriteria(x => x.Id == userId);

            Select(x => new EntranceCashierInfoResponse
            {
                UserId = x.Id,
                CashierName = x.Name,
                Username = x.Username,

                RoleId = x.RoleId,
                RoleNameEn = x.Role.NameEn,
                RoleNameAr = x.Role.NameAr
            });
        }
    }
}