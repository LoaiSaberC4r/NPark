using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport
{
    internal sealed class GetPendingExceedCollectionsSpec
        : Specification<TicketExceedTransaction, PendingExceedCollectionResponse>
    {
        public GetPendingExceedCollectionsSpec(
            DateTime fromDate,
            DateTime toDateExclusive)
        {
            AddCriteria(x =>
                x.SuperVisorCollectorId == null &&
                x.CreatedAt >= fromDate &&
                x.CreatedAt < toDateExclusive);

            Select(x => new PendingExceedCollectionResponse
            {
                TransactionId = x.Id,

                TicketIdSnapshot = x.TicketIdSnapshot,
                TicketUniqueCode = x.TicketUniqueCode,
                TicketStartDate = x.TicketStartDate,
                TicketEndDate = x.TicketEndDate,

                ExceedAmount = x.ExceedAmount,

                VehicleNumber = x.VehicleNumber,

                ExceedCreatedById = x.UserCollectId,
                ExceedCreatedByName = x.UserCollector.Name,

                CreatedAt = x.CreatedAt,

                GateId = x.GateId,
                GateNumber = x.Gate.GateNumber
            });
        }
    }
}