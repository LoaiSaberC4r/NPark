using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport
{
    internal sealed class GetSupervisorReportInfoSpec
        : Specification<User, SupervisorReportInfoResponse>
    {
        public GetSupervisorReportInfoSpec(Guid supervisorId)
        {
            AddCriteria(x => x.Id == supervisorId);

            Select(x => new SupervisorReportInfoResponse
            {
                SupervisorId = x.Id,
                SupervisorName = x.Name,
                Username = x.Username,
                RoleId = x.RoleId,
                RoleNameEn = x.Role.NameEn,
                RoleNameAr = x.Role.NameAr
            });
        }
    }
}