using BuildingBlock.Domain.Enums;
using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.TicketsManagement.Query.GetTicketDetails;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.GetExitTicketForAdmin
{
    internal sealed class GetTicketDetailsSpecification
         : Specification<Ticket, GetTicketDetailsQueryResponse>
    {
        public GetTicketDetailsSpecification(GetTicketDetailsQuery request)
        {
            Include(x => x.TicketAttachments);

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var searchText = request.SearchText.Trim();

                AddCriteria(x =>
                    x.UniqueCode.Contains(searchText) ||
                    (x.VehicleNumber != null && x.VehicleNumber.Contains(searchText)) ||
                    (x.CardNumber != null && x.CardNumber.Contains(searchText)) ||
                    (x.SubscriberNationalId != null && x.SubscriberNationalId.Contains(searchText)));
            }

            if (request.FromDate.HasValue)
            {
                var fromDate = request.FromDate.Value.Date;
                AddCriteria(x => x.StartDate >= fromDate);
            }

            if (request.ToDate.HasValue)
            {
                var toDateExclusive = request.ToDate.Value.Date.AddDays(1);
                AddCriteria(x => x.StartDate < toDateExclusive);
            }

            if (request.GateId.HasValue)
            {
                AddCriteria(x => x.GateId == request.GateId.Value);
            }

            if (request.IsCollected.HasValue)
            {
                AddCriteria(x => x.IsCollected == request.IsCollected.Value);
            }

            if (request.IsSubscriber.HasValue)
            {
                AddCriteria(x => x.IsSubscriber == request.IsSubscriber.Value);
            }

            if (request.OrderSort == OrderSort.Newest)
            {
                AddOrderByDescending(x => x.StartDate);
            }
            else
            {
                AddOrderBy(x => x.StartDate);
            }

            ApplyPaging(request.PageNumber, request.PageSize);

            Select(x => new GetTicketDetailsQueryResponse
            {
                TicketId = x.Id,

                StartDate = x.StartDate,
                EndDate = x.EndDate,

                Price = x.Price,
                ExceedPrice = x.ExceedPrice,
                TotalPrice = x.TotalPrice,

                VehicleNumber = x.VehicleNumber,

                IsCollected = x.IsCollected,
                CollectedBy = x.CollectedBy,
                CollectedDate = x.CollectedDate,

                UniqueCode = x.UniqueCode,

                GateId = x.GateId,
                ExitGateId = x.ExitGateId,
                UserId = x.UserId,

                IsCashierCollected = x.IsCashierCollected,

                IsSubscriber = x.IsSubscriber,
                SubscriberNationalId = x.SubscriberNationalId,
                CardNumber = x.CardNumber,

                Attachments = x.TicketAttachments
                    .Select(a => new TicketAttachmentDetailsResponse
                    {
                        AttachmentId = a.Id,
                        FilePath = "Media/" + a.FilePath
                    })
                    .ToList()
            });

            UseNoTracking();
        }
    }
}