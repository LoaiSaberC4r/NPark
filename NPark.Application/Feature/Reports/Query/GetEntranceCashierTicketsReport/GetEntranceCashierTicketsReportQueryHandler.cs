using NPark.Application.Services;
using NPark.Domain.Enums;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;
using NPark.Domain.FileNames;
using NPark.Domain.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetEntranceCashierTicketsReport
{
    internal sealed class GetEntranceCashierTicketsReportQueryHandler
        : IQueryHandler<GetEntranceCashierTicketsReportQuery, EntranceCashierTicketsReportResponse>
    {
        private readonly IUnitOfWork _db;
        private readonly ParkingBusinessClock _clock;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<TicketArchive> _ticketArchiveRepository;

        public GetEntranceCashierTicketsReportQueryHandler(
            IGenericRepository<User> userRepository,
            IGenericRepository<Ticket> ticketRepository,
            IGenericRepository<TicketArchive> ticketArchiveRepository, IUnitOfWork db, ParkingBusinessClock clock)
        {
            _db = db;
            _clock = clock;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _ticketArchiveRepository = ticketArchiveRepository ?? throw new ArgumentNullException(nameof(ticketArchiveRepository));
        }

        public async Task<Result<EntranceCashierTicketsReportResponse>> Handle(
            GetEntranceCashierTicketsReportQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cashier = await _userRepository.FirstOrDefaultWithSpecAsync(
                new GetEntranceCashierInfoSpec(request.UserId),
                cancellationToken);

            if (cashier is null)
            {
                return Result<EntranceCashierTicketsReportResponse>.Fail(
                    new Error(
                        Code: "EntranceCashierReport.UserNotFound",
                        Message: ErrorMessage.User_NotFound,
                        Type: ErrorType.NotFound));
            }

            if (cashier.RoleId != AppRoleIds.EntranceCashier)
            {
                return Result<EntranceCashierTicketsReportResponse>.Fail(
                    new Error(
                        Code: "EntranceCashierReport.UserMustBeEntranceCashier",
                        Message: ErrorMessage.User_MustBeEntranceCashier,
                        Type: ErrorType.Validation));
            }

            var fromDate = request.From.Date;
            var toDate = request.To.Date;
            var toDateExclusive = toDate.AddDays(1);

            var unsettledExits = await _db.ListWithSpecAsync(new Matching<GateOperation>(x =>
                x.Status == GateOperationStatus.Acknowledged && x.Type == GateOperationType.Exit &&
                x.TicketIdSnapshot != null), cancellationToken);
            var acknowledgedIds = unsettledExits.Select(x => x.TicketIdSnapshot!.Value).ToArray();
            var issued = await _db.ListWithSpecAsync(new Matching<Ticket>(x => x.UserId == request.UserId &&
                x.StartDate >= fromDate && x.StartDate < toDateExclusive &&
                (x.EntryConfirmationStatus == EntryConfirmationStatus.Pending || acknowledgedIds.Contains(x.Id))),
                cancellationToken);

            var utcStart = _clock.ToUtc(fromDate);
            var utcEnd = _clock.ToUtc(toDateExclusive);
            var paidActive = await _db.ListWithSpecAsync(new Matching<Ticket>(x => x.BasePaymentCashierId == request.UserId &&
                x.BasePaymentConfirmedAtUtc >= utcStart && x.BasePaymentConfirmedAtUtc < utcEnd), cancellationToken);
            var paidArchived = await _db.ListWithSpecAsync(new Matching<TicketArchive>(x => x.BasePaymentCashierId == request.UserId &&
                x.BasePaymentConfirmedAtUtc >= utcStart && x.BasePaymentConfirmedAtUtc < utcEnd), cancellationToken);
            var paidArchivedIds = paidArchived.Select(x => x.Id).ToHashSet();

            var insideTickets = await _ticketRepository.ListWithSpecAsync(
                new GetEntranceCashierInsideTicketsSpec(
                    request.UserId,
                    fromDate,
                    toDateExclusive, acknowledgedIds),
                cancellationToken);

            var outsideTickets = await _ticketArchiveRepository.ListWithSpecAsync(
                new GetEntranceCashierOutsideTicketsSpec(
                    request.UserId,
                    fromDate,
                    toDateExclusive),
                cancellationToken);

            var insideTicketsList = insideTickets?.ToList()
                ?? new List<EntranceCashierInsideTicketResponse>();

            var outsideTicketsList = outsideTickets?.ToList()
                ?? new List<EntranceCashierOutsideTicketResponse>();

            var insideParkTicketsCount = insideTicketsList.Count;
            var outsideParkTicketsCount = outsideTicketsList.Count;

            var insideParkTotalBasePrice = insideTicketsList.Sum(x => x.Price);
            var outsideParkTotalBasePrice = outsideTicketsList.Sum(x => x.Price);

            var insideParkTotalExceedPrice = insideTicketsList.Sum(x => x.ExceedPrice);
            var outsideParkTotalExceedPrice = outsideTicketsList.Sum(x => x.ExceedPrice);

            var insideParkTotalAmount = insideTicketsList.Sum(x => x.TotalPrice);
            var outsideParkTotalAmount = outsideTicketsList.Sum(x => x.TotalPrice);

            var insideParkExceededTicketsCount = insideTicketsList.Count(x => x.ExceedPrice > 0);
            var outsideParkExceededTicketsCount = outsideTicketsList.Count(x => x.ExceedPrice > 0);

            var response = new EntranceCashierTicketsReportResponse
            {
                UserId = cashier.UserId,
                CashierName = cashier.CashierName,
                Username = cashier.Username,

                RoleId = cashier.RoleId,
                RoleNameEn = cashier.RoleNameEn,
                RoleNameAr = cashier.RoleNameAr,

                From = fromDate,
                To = toDate,

                Summary = new EntranceCashierTicketsReportSummaryResponse
                {
                    TotalEnteredTicketsCount = insideParkTicketsCount + outsideParkTicketsCount,

                    PendingEntryTicketsCount = issued.Count(x => x.EntryConfirmationStatus == EntryConfirmationStatus.Pending),
                    AcknowledgedExitPendingArchiveCount = issued.Count(x => acknowledgedIds.Contains(x.Id)),
                    ConfirmedBasePaidToCashier = paidActive.Where(x => !paidArchivedIds.Contains(x.Id)).Sum(x => x.Price)
                        + paidArchived.Sum(x => x.Price),
                    InsideParkTicketsCount = insideParkTicketsCount,
                    OutsideParkTicketsCount = outsideParkTicketsCount,

                    InsideParkTotalBasePrice = insideParkTotalBasePrice,
                    OutsideParkTotalBasePrice = outsideParkTotalBasePrice,
                    TotalBasePrice = insideParkTotalBasePrice + outsideParkTotalBasePrice,

                    InsideParkTotalExceedPrice = insideParkTotalExceedPrice,
                    OutsideParkTotalExceedPrice = outsideParkTotalExceedPrice,
                    TotalExceedPrice = insideParkTotalExceedPrice + outsideParkTotalExceedPrice,

                    InsideParkTotalAmount = insideParkTotalAmount,
                    OutsideParkTotalAmount = outsideParkTotalAmount,
                    TotalAmount = insideParkTotalAmount + outsideParkTotalAmount,

                    InsideParkExceededTicketsCount = insideParkExceededTicketsCount,
                    OutsideParkExceededTicketsCount = outsideParkExceededTicketsCount,
                    TotalExceededTicketsCount = insideParkExceededTicketsCount + outsideParkExceededTicketsCount
                },

                InsideParkTickets = insideTicketsList,
                OutsideParkTickets = outsideTicketsList
            };

            return Result<EntranceCashierTicketsReportResponse>.Ok(response);
        }
    }
}