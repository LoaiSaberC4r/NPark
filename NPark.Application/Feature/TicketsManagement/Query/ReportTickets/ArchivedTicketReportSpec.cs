using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.TicketsManagement.Query.ReportTickets;
using NPark.Domain.Entities;
using NPark.Domain.FileNames;

namespace NPark.Application.Specifications.TicketSpecification
{
    internal sealed class ArchivedTicketReportSpec : Specification<TicketArchive, ReportTicketsResponse>
    {
        public ArchivedTicketReportSpec(ReportTicketsQuery request)
        {
            Include(x => x.User);
            Include(x => x.UserCollector);
            Include(x => x.ParkingGate);
            Include(x => x.TicketArchiveAttachments);
            Include(x => x.ExitParkingGate);

            var fromDate = request.FromDate.Date;
            var toDateExclusive = request.ToDate.Date.AddDays(1);

            AddCriteria(x =>
                x.StartDate >= fromDate &&
                x.StartDate < toDateExclusive);

            var search = TicketReportSearchHelper.Build(request.SearchTerm);

            if (search.HasSearch)
            {
                ApplySearchCriteria(search);
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
                TicketNumber = x.UniqueCode,

                IsInsideParking = false,
                TicketStatus = ReportTicketStatusNames.Exited,
                EntryGateNumber = x.ParkingGate == null ? null : x.ParkingGate.GateNumber,
                ExitGateNumber = x.ExitParkingGate == null ? null : x.ExitParkingGate.GateNumber,
                ExceedPrice = x.ExceedPrice,
                CreatedByUserId = x.UserId,
                CreatedByUserName = x.User == null ? string.Empty : x.User.Name,

                Attachments = x.TicketArchiveAttachments
                    .Select(a => new ReportTicketAttachmentResponse
                    {
                        ImagePath = "Media/" + FileNames.TicketAttachments + "/" + a.FilePath
                    })
                    .ToList()
            });
        }

        private void ApplySearchCriteria(TicketReportSearchInfo search)
        {
            var term = search.SearchTerm!;

            var dateStart = search.SearchDate;
            var dateEndExclusive = search.SearchDate?.AddDays(1);

            AddCriteria(x =>
                // -----------------------------
                // Ticket Id / Guid Fields
                // -----------------------------
                (search.SearchGuid.HasValue && x.Id == search.SearchGuid.Value) ||
                (search.SearchGuid.HasValue && x.GateId == search.SearchGuid.Value) ||
                (search.SearchGuid.HasValue && x.ExitGateId == search.SearchGuid.Value) ||
                // Exit Gate Number

                // -----------------------------
                (search.SearchInt.HasValue &&
                x.ExitParkingGate != null &&
                x.ExitParkingGate.GateNumber == search.SearchInt.Value) ||

                // -----------------------------
                // Text Fields
                // -----------------------------
                (!string.IsNullOrEmpty(x.VehicleNumber) && x.VehicleNumber.Contains(term)) ||
                (!string.IsNullOrEmpty(x.UniqueCode) && x.UniqueCode.Contains(term)) ||
                (!string.IsNullOrEmpty(x.CardNumber) && x.CardNumber.Contains(term)) ||
                (!string.IsNullOrEmpty(x.SubscriberNationalId) && x.SubscriberNationalId.Contains(term)) ||

                // -----------------------------
                // Users
                // -----------------------------
                (x.User != null && x.User.Name.Contains(term)) ||
                (x.UserCollector != null && x.UserCollector.Name.Contains(term)) ||

                // -----------------------------
                // Entry Gate Number
                // -----------------------------
                (search.SearchInt.HasValue &&
                    x.ParkingGate != null &&
                    x.ParkingGate.GateNumber == search.SearchInt.Value) ||

                // -----------------------------
                // Status Keywords
                // -----------------------------
                search.WantsExited ||

                // -----------------------------
                // Subscriber / Walk-in Keywords
                // -----------------------------
                (search.WantsSubscriber && x.IsSubscriber) ||
                (search.WantsWalkIn && !x.IsSubscriber) ||

                // -----------------------------
                // Collection Keywords
                // -----------------------------
                (search.WantsCollected && x.IsCollected) ||
                (search.WantsPendingCollection && !x.IsCollected) ||

                // -----------------------------
                // Date Search
                // -----------------------------
                (dateStart.HasValue &&
                    x.StartDate >= dateStart.Value &&
                    x.StartDate < dateEndExclusive!.Value) ||

                (dateStart.HasValue &&
                    x.EndDate >= dateStart.Value &&
                    x.EndDate < dateEndExclusive!.Value) ||

                (dateStart.HasValue &&
                    x.CollectedDate >= dateStart.Value &&
                    x.CollectedDate < dateEndExclusive!.Value)
            );
        }
    }
}