using BuildingBlock.Domain.Enums;
using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.SearchTicket
{
    public sealed class SearchTicketQuerySpecification
       : Specification<Ticket, SearchTicketQueryResponse>
    {
        public SearchTicketQuerySpecification(SearchTicketQuery query)
        {
            if (query.StartDate.HasValue)
            {
                var from = query.StartDate.Value.Date;

                // From selected date 00:00:00
                // To end of today by using tomorrow 00:00:00 as exclusive limit
                var to = DateTime.Now.Date.AddDays(1);

                AddCriteria(x => x.StartDate >= from && x.StartDate < to);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var searchText = query.SearchText.Trim();
                var compactSearchText = searchText.Replace(" ", string.Empty);

                AddCriteria(x =>
                    (x.VehicleNumber != null &&
                        (
                            x.VehicleNumber.Contains(searchText) ||
                            x.VehicleNumber.Replace(" ", string.Empty).Contains(compactSearchText)
                        )
                    )
                    ||
                    (x.SubscriberNationalId != null &&
                        x.SubscriberNationalId.Contains(searchText))
                    ||
                    (x.CardNumber != null &&
                        x.CardNumber.Contains(searchText))
                    ||
                    x.UniqueCode.Contains(searchText)
                );
            }

            if (query.OrderSort == OrderSort.Oldest)
            {
                AddOrderBy(x => x.StartDate);
            }
            else
            {
                AddOrderByDescending(x => x.StartDate);
            }

            EnableTotalCount();
            ApplyPaging(query.PageNumber, query.PageSize);

            Select(x => new SearchTicketQueryResponse
            {
                Id = x.Id,
                StartDate = x.StartDate,
                VehicleNumber = x.VehicleNumber,
                CardNumber = x.CardNumber,
                UniqueCode = x.UniqueCode,
                IsSubscribe = x.IsSubscriber,
                Price = x.Price
            });
        }
    }
}