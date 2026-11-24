using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionReport
{
    public sealed class UsersByIdsSpec : Specification<User>
    {
        public UsersByIdsSpec(IEnumerable<Guid> ids)
        {
            var userIds = ids
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            AddCriteria(x => userIds.Contains(x.Id));
        }
    }
}