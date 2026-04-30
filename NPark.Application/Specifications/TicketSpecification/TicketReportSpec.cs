using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.TicketsManagement.Query.ReportTickets;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.TicketSpecification
{
    internal sealed class TicketReportSpec : Specification<TicketArchive, ReportTicketsResponse>
    {
        public TicketReportSpec(ReportTicketsQuery request)
        {
            Include(x => x.UserCollector);

            var fromDate = request.FromDate.Date;
            var toDateExclusive = request.ToDate.Date.AddDays(1);

            AddCriteria(x =>
                x.StartDate >= fromDate &&
                x.StartDate < toDateExclusive);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim();

                AddCriteria(x =>
                    (x.VehicleNumber != null && x.VehicleNumber.Contains(searchTerm)) ||
                    (x.UserCollector != null && x.UserCollector.Name.Contains(searchTerm)));
            }

            Select(x => new ReportTicketsResponse
            {
                TicketId = x.Id,
                EnterDate = x.StartDate,
                ExitDate = x.EndDate,
                VehicleNumber = x.VehicleNumber,
                IsSubscribed = x.IsSubscriber,
                Price = x.Price,
                CollectorName = x.UserCollector == null ? string.Empty : x.UserCollector.Name,
                TicketNumber = x.UniqueCode
            });
        }
    }
}