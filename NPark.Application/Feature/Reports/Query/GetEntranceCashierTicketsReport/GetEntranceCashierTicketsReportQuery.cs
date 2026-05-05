using BuildingBlock.Application.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetEntranceCashierTicketsReport
{
    public sealed record GetEntranceCashierTicketsReportQuery
       : IQuery<EntranceCashierTicketsReportResponse>
    {
        public Guid UserId { get; init; }
        public DateTime From { get; init; }
        public DateTime To { get; init; }
    }
}