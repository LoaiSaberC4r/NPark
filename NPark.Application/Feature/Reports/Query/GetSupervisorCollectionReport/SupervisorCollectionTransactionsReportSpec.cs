using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionReport
{
    public sealed class SupervisorCollectionTransactionsReportSpec
       : Specification<TicketCollectionTransaction>
    {
        public SupervisorCollectionTransactionsReportSpec(
            Guid supervisorId,
            DateTime fromDate,
            DateTime toDate)
        {
            var from = fromDate.Date;
            var toExclusive = toDate.Date.AddDays(1);

            Include(x => x.Gate);
            Include(x => x.Collector);
            Include(x => x.Lines);
            Include(x => x.Tickets);

            AddCriteria(x =>
                x.CollectedBy == supervisorId &&
                x.CollectedAt >= from &&
                x.CollectedAt < toExclusive);

            AddOrderBy(x => x.CollectedAt);
        }
    }
}