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

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport
{
    internal sealed class GetSupervisorCollectionsReportQueryHandler
        : IQueryHandler<GetSupervisorCollectionsReportQuery, SupervisorCollectionsReportResponse>
    {
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<TicketArchive> _ticketArchiveRepository;
        private readonly IGenericRepository<TicketExceedTransaction> _ticketExceedTransactionRepository;

        public GetSupervisorCollectionsReportQueryHandler(
            IGenericRepository<User> userRepository,
            IGenericRepository<Ticket> ticketRepository,
            IGenericRepository<TicketArchive> ticketArchiveRepository,
            IGenericRepository<TicketExceedTransaction> ticketExceedTransactionRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _ticketArchiveRepository = ticketArchiveRepository ?? throw new ArgumentNullException(nameof(ticketArchiveRepository));
            _ticketExceedTransactionRepository = ticketExceedTransactionRepository ?? throw new ArgumentNullException(nameof(ticketExceedTransactionRepository));
        }

        public async Task<Result<SupervisorCollectionsReportResponse>> Handle(
            GetSupervisorCollectionsReportQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var supervisor = await _userRepository.FirstOrDefaultWithSpecAsync(
                new GetSupervisorReportInfoSpec(request.SupervisorId),
                cancellationToken);

            if (supervisor is null)
            {
                return Result<SupervisorCollectionsReportResponse>.Fail(
                    new Error(
                        Code: "SupervisorCollectionsReport.SupervisorNotFound",
                        Message: ErrorMessage.Supervisor_NotFound,
                        Type: ErrorType.NotFound));
            }

            if (supervisor.RoleId != AppRoleIds.Supervisor)
            {
                return Result<SupervisorCollectionsReportResponse>.Fail(
                    new Error(
                        Code: "SupervisorCollectionsReport.UserMustBeSupervisor",
                        Message: ErrorMessage.User_MustBeSupervisor,
                        Type: ErrorType.Validation));
            }

            var fromDate = request.From.Date;
            var toDate = request.To.Date;
            var toDateExclusive = toDate.AddDays(1);

            var activeTicketCollections = await _ticketRepository.ListWithSpecAsync(
                new GetActiveTicketCollectionsSpec(fromDate, toDateExclusive),
                cancellationToken);

            var archivedTicketCollections = await _ticketArchiveRepository.ListWithSpecAsync(
                new GetArchivedTicketCollectionsSpec(fromDate, toDateExclusive),
                cancellationToken);

            var supervisorExceedCollections = await _ticketExceedTransactionRepository.ListWithSpecAsync(
                new GetSupervisorExceedCollectionsSpec(
                    request.SupervisorId,
                    fromDate,
                    toDateExclusive),
                cancellationToken);

            var pendingExceedCollections = await _ticketExceedTransactionRepository.ListWithSpecAsync(
                new GetPendingExceedCollectionsSpec(fromDate, toDateExclusive),
                cancellationToken);

            var activeTickets = activeTicketCollections?.ToList()
                ?? new List<ActiveTicketCollectionResponse>();

            var archivedTickets = archivedTicketCollections?.ToList()
                ?? new List<ArchivedTicketCollectionResponse>();

            var collectedExceeds = supervisorExceedCollections?.ToList()
                ?? new List<SupervisorExceedCollectionResponse>();

            var pendingExceeds = pendingExceedCollections?.ToList()
                ?? new List<PendingExceedCollectionResponse>();

            var activeTicketCollectionsAmount = activeTickets.Sum(x => x.CollectionAmount);
            var archivedTicketCollectionsAmount = archivedTickets.Sum(x => x.CollectionAmount);

            var ticketCollectionsAmount =
                activeTicketCollectionsAmount + archivedTicketCollectionsAmount;

            var supervisorExceedCollectionsAmount =
                collectedExceeds.Sum(x => x.ExceedAmount);

            var pendingExceedCollectionsAmount =
                pendingExceeds.Sum(x => x.ExceedAmount);

            var grandTotalCollectionsAmount =
                ticketCollectionsAmount + supervisorExceedCollectionsAmount;

            var collectorsSummary = BuildTicketCollectorsSummary(
                activeTickets,
                archivedTickets);

            var response = new SupervisorCollectionsReportResponse
            {
                Supervisor = supervisor,

                Period = new SupervisorCollectionsPeriodResponse
                {
                    From = fromDate,
                    To = toDate
                },

                Summary = new SupervisorCollectionsSummaryResponse
                {
                    TicketCollectionsCount = activeTickets.Count + archivedTickets.Count,

                    ActiveTicketCollectionsCount = activeTickets.Count,
                    ArchivedTicketCollectionsCount = archivedTickets.Count,

                    ActiveTicketCollectionsAmount = activeTicketCollectionsAmount,
                    ArchivedTicketCollectionsAmount = archivedTicketCollectionsAmount,
                    TicketCollectionsAmount = ticketCollectionsAmount,

                    SupervisorExceedCollectionsCount = collectedExceeds.Count,
                    SupervisorExceedCollectionsAmount = supervisorExceedCollectionsAmount,

                    PendingExceedCollectionsCount = pendingExceeds.Count,
                    PendingExceedCollectionsAmount = pendingExceedCollectionsAmount,

                    GrandTotalCollectionsAmount = grandTotalCollectionsAmount
                },

                TicketCollectorsSummary = collectorsSummary,

                ActiveTicketCollections = activeTickets,
                ArchivedTicketCollections = archivedTickets,

                SupervisorExceedCollections = collectedExceeds,
                PendingExceedCollections = pendingExceeds
            };

            return Result<SupervisorCollectionsReportResponse>.Ok(response);
        }

        private static IReadOnlyList<TicketCollectorSummaryResponse> BuildTicketCollectorsSummary(
            IReadOnlyCollection<ActiveTicketCollectionResponse> activeTickets,
            IReadOnlyCollection<ArchivedTicketCollectionResponse> archivedTickets)
        {
            var activeSummary = activeTickets
                .Where(x => x.CollectedBy.HasValue)
                .GroupBy(x => new
                {
                    CollectorId = x.CollectedBy!.Value,
                    CollectorName = x.CollectorName ?? string.Empty
                })
                .Select(x => new
                {
                    x.Key.CollectorId,
                    x.Key.CollectorName,
                    ActiveTicketsCount = x.Count(),
                    ArchivedTicketsCount = 0,
                    ActiveTicketsAmount = x.Sum(t => t.CollectionAmount),
                    ArchivedTicketsAmount = 0m
                });

            var archivedSummary = archivedTickets
                .Where(x => x.CollectedBy.HasValue)
                .GroupBy(x => new
                {
                    CollectorId = x.CollectedBy!.Value,
                    CollectorName = x.CollectorName ?? string.Empty
                })
                .Select(x => new
                {
                    x.Key.CollectorId,
                    x.Key.CollectorName,
                    ActiveTicketsCount = 0,
                    ArchivedTicketsCount = x.Count(),
                    ActiveTicketsAmount = 0m,
                    ArchivedTicketsAmount = x.Sum(t => t.CollectionAmount)
                });

            return activeSummary
                .Concat(archivedSummary)
                .GroupBy(x => new
                {
                    x.CollectorId,
                    x.CollectorName
                })
                .Select(x => new TicketCollectorSummaryResponse
                {
                    CollectorId = x.Key.CollectorId,
                    CollectorName = x.Key.CollectorName,

                    ActiveTicketsCount = x.Sum(t => t.ActiveTicketsCount),
                    ArchivedTicketsCount = x.Sum(t => t.ArchivedTicketsCount),
                    TotalTicketsCount = x.Sum(t => t.ActiveTicketsCount + t.ArchivedTicketsCount),

                    ActiveTicketsAmount = x.Sum(t => t.ActiveTicketsAmount),
                    ArchivedTicketsAmount = x.Sum(t => t.ArchivedTicketsAmount),
                    TotalAmount = x.Sum(t => t.ActiveTicketsAmount + t.ArchivedTicketsAmount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();
        }
    }
}