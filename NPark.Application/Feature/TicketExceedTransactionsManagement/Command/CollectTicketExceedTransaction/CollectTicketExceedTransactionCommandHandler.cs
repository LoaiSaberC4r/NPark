using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.Specification;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.FileNames;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction
{
    internal sealed class CollectTicketExceedTransactionCommandHandler
     : ICommandHandler<CollectTicketExceedTransactionCommand, CollectTicketExceedTransactionResponse>
    {
        private readonly IGenericRepository<TicketExceedTransaction> _ticketExceedTransactionRepository;
        private readonly IGenericRepository<ParkingGate> _parkingGateRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly ITokenReader _tokenReader;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CollectTicketExceedTransactionCommandHandler> _logger;

        public CollectTicketExceedTransactionCommandHandler(
            IGenericRepository<TicketExceedTransaction> ticketExceedTransactionRepository,
            IGenericRepository<ParkingGate> parkingGateRepository,
            IGenericRepository<User> userRepository,
            ITokenReader tokenReader,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogger auditLogger,
            ILogger<CollectTicketExceedTransactionCommandHandler> logger)
        {
            _ticketExceedTransactionRepository = ticketExceedTransactionRepository
                ?? throw new ArgumentNullException(nameof(ticketExceedTransactionRepository));

            _parkingGateRepository = parkingGateRepository
                ?? throw new ArgumentNullException(nameof(parkingGateRepository));

            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));

            _tokenReader = tokenReader
                ?? throw new ArgumentNullException(nameof(tokenReader));

            _httpContextAccessor = httpContextAccessor
                ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            _auditLogger = auditLogger
                ?? throw new ArgumentNullException(nameof(auditLogger));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<CollectTicketExceedTransactionResponse>> Handle(
            CollectTicketExceedTransactionCommand request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var tokenInfo = _httpContextAccessor.HttpContext?.ReadToken(_tokenReader);

                if (tokenInfo is null || !tokenInfo.GateId.HasValue || !tokenInfo.UserId.HasValue)
                {
                    _logger.LogWarning(
                        "Token info is missing GateId/UserId while collecting ticket exceed transactions.");

                    return Result<CollectTicketExceedTransactionResponse>.Fail(
                        new Error(
                            Code: "Token.GateOrUser.NotFound",
                            Message: ErrorMessage.TokenInfo_Missing,
                            Type: ErrorType.NotFound));
                }

                var gateId = tokenInfo.GateId.Value;
                var supervisorId = tokenInfo.UserId.Value;
                if (tokenInfo.Role != "Supervisor")
                {
                    return Result<CollectTicketExceedTransactionResponse>.Fail(
                        new Error(
                            Code: "User.NotSupervisor",
                            Message: ErrorMessage.Supervisor_OnlyCanCollectTicketExceedTransactions,
                            Type: ErrorType.Security));
                }

                var gate = await _parkingGateRepository.GetByIdAsync(gateId, cancellationToken);

                if (gate is null)
                {
                    return Result<CollectTicketExceedTransactionResponse>.Fail(
                        new Error(
                            Code: "Gate.NotFound",
                            Message: ErrorMessage.GateNotFound,
                            Type: ErrorType.NotFound));
                }

                var supervisor = await _userRepository.GetByIdAsync(supervisorId, cancellationToken);

                if (supervisor is null)
                {
                    return Result<CollectTicketExceedTransactionResponse>.Fail(
                        new Error(
                            Code: "User.NotFound",
                            Message: ErrorMessage.User_NotFound,
                            Type: ErrorType.NotFound));
                }

                if (supervisor.RoleId != SystemRoles.SupervisorRoleId)
                {
                    _logger.LogWarning(
                        "User {UserId} tried to collect ticket exceed transactions but is not supervisor.",
                        supervisorId);

                    return Result<CollectTicketExceedTransactionResponse>.Fail(
                        new Error(
                            Code: "User.NotSupervisor",
                            Message: ErrorMessage.Supervisor_OnlyCanCollectTicketExceedTransactions,
                            Type: ErrorType.Security));
                }

                Specification<TicketExceedTransaction> spec =
                    new UncollectedTicketExceedTransactionsByGateSpec(gateId);

                var transactions = await _ticketExceedTransactionRepository
                    .ListWithSpecAsync(spec, cancellationToken);

                var transactionList = transactions?.ToList() ?? new List<TicketExceedTransaction>();

                if (transactionList.Count == 0)
                {
                    _logger.LogWarning(
                        "No uncollected ticket exceed transactions found for gate {GateId}.",
                        gateId);

                    return Result<CollectTicketExceedTransactionResponse>.Fail(
                        new Error(
                            Code: "TicketExceedTransactions.NoneToCollect",
                            Message: ErrorMessage.TicketExceedTransactions_NoneToCollect,
                            Type: ErrorType.Conflict));
                }

                foreach (var transaction in transactionList)
                {
                    transaction.SetSupervisorCollected(supervisorId);
                }

                var collectedAt = DateTime.Now;

                var response = new CollectTicketExceedTransactionResponse
                {
                    TotalTransactionsCollected = transactionList.Count,
                    TotalExceedAmountCollected = transactionList.Sum(x => x.ExceedAmount),
                    TicketExceedCollectDetails = transactionList
                        .Select(x => new TicketExceedCollectDetails
                        {
                            TransactionId = x.Id,
                            TicketIdSnapshot = x.TicketIdSnapshot,
                            TicketUniqueCode = x.TicketUniqueCode,
                            TransactionCreatedAt = x.CreatedAt,
                            SupervisorCollectedAt = collectedAt,
                            TicketPrice = x.TicketPrice,
                            TicketOldExceedPrice = x.TicketOldExceedPrice,
                            ExceedAmount = x.ExceedAmount,
                            TotalPriceAfterExceed = x.TotalPriceAfterExceed,
                            VehicleNumber = x.VehicleNumber,
                            CardNumber = x.CardNumber,
                            IsSubscriber = x.IsSubscriber
                        })
                        .ToList()
                };

                await _ticketExceedTransactionRepository.SaveChangesAsync(cancellationToken);

                await SafeAuditAsync(
                    response,
                    supervisorId,
                    gateId,
                    cancellationToken);

                return Result<CollectTicketExceedTransactionResponse>.Ok(response);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("CollectTicketExceedTransaction operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while collecting ticket exceed transactions.");

                return Result<CollectTicketExceedTransactionResponse>.Fail(
                    new Error(
                        Code: "TicketExceedTransactions.Collect.Unexpected",
                        Message: ErrorMessage.TicketExceedTransactions_CollectUnexpected,
                        Type: ErrorType.Infrastructure));
            }
        }

        private async Task SafeAuditAsync(
            CollectTicketExceedTransactionResponse response,
            Guid supervisorId,
            Guid gateId,
            CancellationToken cancellationToken)
        {
            try
            {
                await _auditLogger.LogAsync(
                    new AuditLogEntry(
                        EventName: "CollectTicketExceedTransactionsSucceeded",
                        EventCategory: "TicketManagement",
                        IsSuccess: true,
                        StatusCode: StatusCodes.Status200OK,
                        UserId: supervisorId,
                        GateId: gateId,
                        Extra: new
                        {
                            response.TotalTransactionsCollected,
                            response.TotalExceedAmountCollected,
                            TicketExceedDetailsCount = response.TicketExceedCollectDetails.Count,
                            SupervisorCollectorId = supervisorId,
                            GateId = gateId
                        }),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Audit logging failed for CollectTicketExceedTransaction. SupervisorId: {SupervisorId}, GateId: {GateId}",
                    supervisorId,
                    gateId);
            }
        }
    }
}