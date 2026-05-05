using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetEntranceCashierTicketsReport
{
    internal sealed record EntranceCashierInfoResponse
    {
        public Guid UserId { get; init; }
        public string CashierName { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;

        public Guid RoleId { get; init; }
        public string RoleNameEn { get; init; } = string.Empty;
        public string RoleNameAr { get; init; } = string.Empty;
    }
}