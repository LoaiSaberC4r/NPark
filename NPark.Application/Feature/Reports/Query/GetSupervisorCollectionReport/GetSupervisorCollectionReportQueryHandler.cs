using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.FileNames;
using NPark.Domain.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.Reports.Query.GetSupervisorCollectionReport
{
    internal sealed class GetSupervisorCollectionReportQueryHandler
        : IQueryHandler<GetSupervisorCollectionReportQuery, SupervisorCollectionReportResponse>
    {
        private readonly IGenericRepository<TicketCollectionTransaction> _collectionTransactionRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly ILogger<GetSupervisorCollectionReportQueryHandler> _logger;

        public GetSupervisorCollectionReportQueryHandler(
            IGenericRepository<TicketCollectionTransaction> collectionTransactionRepository,
            IGenericRepository<User> userRepository,
            ILogger<GetSupervisorCollectionReportQueryHandler> logger)
        {
            _collectionTransactionRepository = collectionTransactionRepository
                ?? throw new ArgumentNullException(nameof(collectionTransactionRepository));

            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<SupervisorCollectionReportResponse>> Handle(
            GetSupervisorCollectionReportQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fromDate = request.FromDate.Date;
                var toDate = request.ToDate.Date;

                if (toDate < fromDate)
                {
                    return Result<SupervisorCollectionReportResponse>.Fail(
                        new Error(
                            Code: "Report.InvalidDateRange",
                            Message: "To date must be greater than or equal from date.",
                            Type: ErrorType.Validation));
                }

                var supervisor = await _userRepository.GetByIdAsync(
                    request.SupervisorId,
                    cancellationToken);

                if (supervisor is null)
                {
                    return Result<SupervisorCollectionReportResponse>.Fail(
                        new Error(
                            Code: "User.NotFound",
                            Message: ErrorMessage.User_NotFound,
                            Type: ErrorType.NotFound));
                }

                if (supervisor.RoleId != SystemRoles.SupervisorRoleId)
                {
                    return Result<SupervisorCollectionReportResponse>.Fail(
                        new Error(
                            Code: "User.NotSupervisor",
                            Message: ErrorMessage.Supervisor_OnlyCanCollectTicketExceedTransactions,
                            Type: ErrorType.Security));
                }

                var spec = new SupervisorCollectionTransactionsReportSpec(
                    supervisorId: request.SupervisorId,
                    fromDate: fromDate,
                    toDate: toDate);

                var transactionsResult = await _collectionTransactionRepository
                    .ListWithSpecAsync(spec, cancellationToken);

                var transactions = transactionsResult?
                    .OrderByDescending(x => x.CollectedAt)
                    .ToList()
                    ?? new List<TicketCollectionTransaction>();

                var responsibleUserIds = transactions
                    .SelectMany(x => x.Lines.Select(line => line.ResponsibleUserId))
                    .Concat(transactions.SelectMany(x => x.Tickets.Select(ticket => ticket.ResponsibleUserId)))
                    .Concat(transactions.Select(x => x.CollectedBy))
                    .Append(request.SupervisorId)
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToArray();

                var users = responsibleUserIds.Length == 0
                    ? new List<User>()
                    : (await _userRepository.ListWithSpecAsync(
                        new UsersByIdsSpec(responsibleUserIds),
                        cancellationToken))?.ToList() ?? new List<User>();

                var userNames = users
                    .GroupBy(x => x.Id)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.Name)
                              .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty);

                var supervisorName = !string.IsNullOrWhiteSpace(supervisor.Name)
                    ? supervisor.Name
                    : GetUserName(userNames, request.SupervisorId);

                var transactionItems = transactions
                    .Select(transaction => new SupervisorCollectionTransactionReportItem
                    {
                        TransactionId = transaction.Id,
                        TransactionTypeId = (int)transaction.Type,
                        TransactionType = transaction.Type.ToString(),

                        GateId = transaction.GateId,
                        GateName = transaction.Gate is not null
                            ? transaction.Gate.GateNumber.ToString()
                            : string.Empty,

                        CollectedBy = transaction.CollectedBy,
                        CollectedByName = GetUserName(userNames, transaction.CollectedBy),

                        CollectedAt = transaction.CollectedAt,
                        TotalItemsCollected = transaction.TotalItemsCollected,
                        TotalAmountCollected = transaction.TotalAmountCollected,

                        Lines = transaction.Lines
                            .Select(line => new SupervisorCollectionLineReportItem
                            {
                                ResponsibleUserId = line.ResponsibleUserId,
                                ResponsibleUserName = GetUserName(userNames, line.ResponsibleUserId),
                                ItemsCount = line.ItemsCount,
                                TotalAmount = line.TotalAmount
                            })
                            .OrderByDescending(line => line.TotalAmount)
                            .ToList(),

                        Tickets = transaction.Tickets
                            .Select(ticket => new SupervisorCollectionTicketReportItem
                            {
                                ItemId = ticket.Id,
                                ResponsibleUserId = ticket.ResponsibleUserId,
                                ResponsibleUserName = GetUserName(userNames, ticket.ResponsibleUserId),
                                Source = ticket.Source.ToString(),
                                TicketIdSnapshot = ticket.TicketIdSnapshot,
                                TicketUniqueCode = ticket.TicketUniqueCode,
                                TicketStartDate = ticket.TicketStartDate,
                                TicketEndDate = ticket.TicketEndDate,
                                OriginalPrice = ticket.OriginalPrice,
                                ExceedAmount = ticket.ExceedAmount,
                                AmountCollected = ticket.AmountCollected,
                                VehicleNumber = ticket.VehicleNumber,
                                CardNumber = ticket.CardNumber,
                                IsSubscriber = ticket.IsSubscriber,
                                SubscriberNationalId = ticket.SubscriberNationalId,
                                TicketGateId = ticket.TicketGateId,
                                TicketExitGateId = ticket.TicketExitGateId
                            })
                            .OrderByDescending(ticket => ticket.TicketStartDate)
                            .ToList()
                    })
                    .ToList();

                var enterTransactions = transactions
                    .Where(x => x.Type == TicketCollectionTransactionType.EnterTickets)
                    .ToList();

                var exceedTransactions = transactions
                    .Where(x => x.Type == TicketCollectionTransactionType.ExceedTransactions)
                    .ToList();

                var response = new SupervisorCollectionReportResponse
                {
                    SupervisorId = request.SupervisorId,
                    SupervisorName = supervisorName,
                    FromDate = fromDate,
                    ToDate = toDate,

                    TotalCollectionTransactions = transactions.Count,
                    TotalItemsCollected = transactions.Sum(x => x.TotalItemsCollected),
                    TotalAmountCollected = transactions.Sum(x => x.TotalAmountCollected),

                    TotalEnterTicketsCollected = enterTransactions.Sum(x => x.TotalItemsCollected),
                    TotalEnterTicketsAmount = enterTransactions.Sum(x => x.TotalAmountCollected),

                    TotalExceedTransactionsCollected = exceedTransactions.Sum(x => x.TotalItemsCollected),
                    TotalExceedAmount = exceedTransactions.Sum(x => x.TotalAmountCollected),

                    Transactions = transactionItems
                };

                return Result<SupervisorCollectionReportResponse>.Ok(response);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "GetSupervisorCollectionReport operation was canceled. SupervisorId: {SupervisorId}",
                    request.SupervisorId);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while getting supervisor collection report. SupervisorId: {SupervisorId}",
                    request.SupervisorId);

                return Result<SupervisorCollectionReportResponse>.Fail(
                    new Error(
                        Code: "TicketCollectionReport.Supervisor.Unexpected",
                        Message: "An unexpected error occurred while getting supervisor collection report.",
                        Type: ErrorType.Infrastructure));
            }
        }

        private static string GetUserName(
            IReadOnlyDictionary<Guid, string> userNames,
            Guid userId)
        {
            return userNames.TryGetValue(userId, out var name)
                ? name
                : string.Empty;
        }
    }
}