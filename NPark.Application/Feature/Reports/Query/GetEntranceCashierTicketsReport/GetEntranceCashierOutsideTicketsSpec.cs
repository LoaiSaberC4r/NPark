using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using NPark.Domain.FileNames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetEntranceCashierTicketsReport
{
    internal sealed class GetEntranceCashierOutsideTicketsSpec
          : Specification<TicketArchive, EntranceCashierOutsideTicketResponse>
    {
        public GetEntranceCashierOutsideTicketsSpec(
            Guid userId,
            DateTime fromDate,
            DateTime toDateExclusive)
        {
            AddCriteria(x =>
                x.UserId == userId &&
                x.StartDate >= fromDate &&
                x.StartDate < toDateExclusive);

            Select(x => new EntranceCashierOutsideTicketResponse
            {
                TicketId = x.Id,
                UniqueCode = x.UniqueCode,

                StartDate = x.StartDate,
                EndDate = x.EndDate,

                Price = x.Price,
                ExceedPrice = x.ExceedPrice,
                TotalPrice = x.Price + x.ExceedPrice,

                VehicleNumber = x.VehicleNumber,

                IsSubscriber = x.IsSubscriber,
                SubscriberNationalId = x.SubscriberNationalId,
                CardNumber = x.CardNumber,

                GateId = x.GateId,
                GateNumber = x.ParkingGate.GateNumber,

                ExitGateId = x.ExitGateId,
                ExitGateNumber = x.ExitParkingGate.GateNumber,

                CollectedBy = x.CollectedBy,
                CollectorName = x.UserCollector != null ? x.UserCollector.Name : null,
                CollectedDate = x.CollectedDate,

                Attachments = x.TicketArchiveAttachments
                    .Select(a => new TicketArchiveAttachmentResponse
                    {
                        AttachmentId = a.Id,
                        FilePath = "Media" + "/" + FileNames.TicketAttachments + "/" + a.FilePath
                    })
                    .ToList()
            });
        }
    }
}