using BuildingBlock.Domain.Enums;
using BuildingBlock.Domain.SharedDto;
using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.TicketsManagement.Query.GetTicketForEntryGate;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.TicketSpecification
{
    internal sealed class SearchTicketArchiveSpecification : Specification<TicketArchive, GetTicketInfo>

    {
        public SearchTicketArchiveSpecification(SearchParameters searchParameters)
        {
            var date = DateTime.Now.Date;
            var start = date;
            var end = date.AddDays(1);
            AddCriteria(t => t.CreatedOnUtc >= start && t.CreatedOnUtc < end);

            if (!string.IsNullOrEmpty(searchParameters.SearchText))
                AddCriteria(t => (t.VehicleNumber != null && t.VehicleNumber.Contains(searchParameters.SearchText)) ||
                t.UniqueCode.Contains(searchParameters.SearchText) ||
                (t.CardNumber != null && t.CardNumber.Contains(searchParameters.SearchText)));

            if (searchParameters.OrderSort == OrderSort.Newest)
            {
                AddOrderBy(x => x.CreatedOnUtc);
            }
            else
            {
                AddOrderByDescending(x => x.CreatedOnUtc);
            }

            Select(x => new GetTicketInfo
            {
                isCollectedByCashier = x.IsCollected,
                Price = x.Price,
                TicketNumber = x.UniqueCode,
                StartDate = x.CreatedOnUtc,
                TicketInfo = x.VehicleNumber
            });
        }
    }
}