using NPark.Domain.Enums;
using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport
{
    internal sealed class GetSupervisorExceedCollectionsSpec
           : Specification<TicketExceedTransaction, SupervisorExceedCollectionResponse>
    {
        public GetSupervisorExceedCollectionsSpec(
            Guid supervisorId,
            DateTime fromDate,
            DateTime toDateExclusive)
        {
            AddCriteria(x =>
                x.PaymentStatus != SanctionPaymentStatus.Assessed &&
                x.SuperVisorCollectorId == supervisorId &&
                x.SuperVisorCollectedAt != null &&
                x.SuperVisorCollectedAt >= fromDate &&
                x.SuperVisorCollectedAt < toDateExclusive);

            Select(x => new SupervisorExceedCollectionResponse
            {
                TransactionId = x.Id,

                TicketIdSnapshot = x.TicketIdSnapshot,
                TicketUniqueCode = x.TicketUniqueCode,
                TicketStartDate = x.TicketStartDate,
                TicketEndDate = x.TicketEndDate,

                ExceedAmount = x.ExceedAmount,

                TicketPrice = x.TicketPrice,
                TicketOldExceedPrice = x.TicketOldExceedPrice,
                TotalPriceAfterExceed = x.TotalPriceAfterExceed,

                VehicleNumber = x.VehicleNumber,
                CardNumber = x.CardNumber,
                IsSubscriber = x.IsSubscriber,
                SubscriberNationalId = x.SubscriberNationalId,

                ExceedCreatedById = x.PaymentCashierId ?? x.UserCollectId,
                ExceedCreatedByName = x.UserCollector.Name,

                SupervisorCollectorId = x.SuperVisorCollectorId!.Value,
                SupervisorCollectorName = x.SuperVisorCollector != null
                    ? x.SuperVisorCollector.Name
                    : string.Empty,

                SupervisorCollectedAt = x.SuperVisorCollectedAt,
                CreatedAt = x.CreatedAt,

                GateId = x.PaymentGateId ?? x.GateId,
                GateNumber = x.Gate.GateNumber,

                TicketGateId = x.TicketGateId,
                TicketExitGateId = x.TicketExitGateId
            });
        }
    }
}