using BuildingBlock.Application.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport
{
    public sealed record GetSupervisorCollectionsReportQuery
     : IQuery<SupervisorCollectionsReportResponse>
    {
        public Guid SupervisorId { get; init; }

        public DateTime From { get; init; }

        public DateTime To { get; init; }
    }
}