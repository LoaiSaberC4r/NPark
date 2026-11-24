using NPark.Domain.Enums;
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
    internal sealed class GetEntranceCashierInsideTicketsSpec
        : Specification<Ticket, EntranceCashierInsideTicketResponse>
    {
        public GetEntranceCashierInsideTicketsSpec(
            Guid userId,
            DateTime fromDate,
            DateTime toDateExclusive, Guid[] acknowledgedExits)
        {
            AddCriteria(x =>
                x.UserId == userId && x.EndDate == null &&
                x.EntryConfirmationStatus != EntryConfirmationStatus.Pending &&
                !acknowledgedExits.Contains(x.Id) &&
                x.StartDate >= fromDate &&
                x.StartDate < toDateExclusive);

            Select(x => new EntranceCashierInsideTicketResponse
            {
                TicketId = x.Id,
                UniqueCode = x.UniqueCode,

                StartDate = x.StartDate,

                Price = x.Price,
                ExceedPrice = x.ExceedPrice,
                TotalPrice = x.Price + x.ExceedPrice,

                VehicleNumber = x.VehicleNumber,

                IsSubscriber = x.IsSubscriber,
                SubscriberNationalId = x.SubscriberNationalId,
                CardNumber = x.CardNumber,

                GateId = x.GateId,
                GateNumber = x.ParkingGate.GateNumber,
                Attachments = x.TicketAttachments
                    .Select(a => new TicketAttachmentResponse
                    {
                        AttachmentId = a.Id,
                        FilePath = "Media" + "/" + FileNames.TicketAttachments + "/" + a.FilePath
                    })
                    .ToList()
            });
        }
    }
}