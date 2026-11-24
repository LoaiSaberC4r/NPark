using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport
{
    internal sealed class GetArchivedTicketCollectionsSpec
         : Specification<TicketArchive, ArchivedTicketCollectionResponse>
    {
        public GetArchivedTicketCollectionsSpec(
            Guid supervisorId,
            DateTime fromDate,
            DateTime toDateExclusive)
        {
            AddCriteria(x =>
                x.IsCollected &&
                x.CollectedBy == supervisorId &&
                x.CollectedDate >= fromDate &&
                x.CollectedDate < toDateExclusive);

            Select(x => new ArchivedTicketCollectionResponse
            {
                TicketId = x.Id,
                UniqueCode = x.UniqueCode,

                StartDate = x.StartDate,
                EndDate = x.EndDate,

                CollectedDate = x.CollectedDate,
                CollectedBy = x.CollectedBy,
                CollectorName = x.UserCollector != null ? x.UserCollector.Name : null,

                CollectionAmount = x.Price,
                ResponsibleCashierId = x.BasePaymentCashierId,
                PaymentGateId = x.BasePaymentGateId,

                VehicleNumber = x.VehicleNumber,

                IsSubscriber = x.IsSubscriber,
                SubscriberNationalId = x.SubscriberNationalId,
                CardNumber = x.CardNumber,

                GateId = x.GateId,
                GateNumber = x.ParkingGate.GateNumber,

                ExitGateId = x.ExitGateId,
                ExitGateNumber = x.ExitParkingGate.GateNumber
            });
        }
    }
}