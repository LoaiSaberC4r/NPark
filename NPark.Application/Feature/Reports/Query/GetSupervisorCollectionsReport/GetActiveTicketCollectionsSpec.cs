using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport
{
    internal sealed class GetActiveTicketCollectionsSpec
        : Specification<Ticket, ActiveTicketCollectionResponse>
    {
        public GetActiveTicketCollectionsSpec(
            DateTime fromDate,
            DateTime toDateExclusive)
        {
            AddCriteria(x =>
                x.IsCollected &&
                x.CollectedBy != null &&
                x.CollectedDate != null &&
                x.CollectedDate >= fromDate &&
                x.CollectedDate < toDateExclusive);

            Select(x => new ActiveTicketCollectionResponse
            {
                TicketId = x.Id,
                UniqueCode = x.UniqueCode,

                StartDate = x.StartDate,
                CollectedDate = x.CollectedDate,
                CollectedBy = x.CollectedBy,
                CollectorName = x.UserCollector != null ? x.UserCollector.Name : null,

                CollectionAmount = x.Price,

                VehicleNumber = x.VehicleNumber,

                IsSubscriber = x.IsSubscriber,
                SubscriberNationalId = x.SubscriberNationalId,
                CardNumber = x.CardNumber,

                GateId = x.GateId,
                GateNumber = x.ParkingGate.GateNumber
            });
        }
    }
}