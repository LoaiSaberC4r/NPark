using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.CollectEnterTickets
{
    internal sealed class CollectEnterTicketsCommandHandler
         : ICommandHandler<CollectEnterTicketsCommand, CollectEnterTicketsResponse>
    {
        private const string TicketSource = "Ticket";
        private const string TicketArchiveSource = "TicketArchive";

        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<TicketArchive> _ticketArchiveRepository;
        private readonly IGenericRepository<ParkingGate> _parkingGateRepository;
        private readonly IGenericRepository<TicketCollectionTransaction> _ticketCollectionTransactionRepository;
        private readonly ITokenReader _tokenReader;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CollectEnterTicketsCommandHandler> _logger;

        public CollectEnterTicketsCommandHandler(
            IGenericRepository<Ticket> ticketRepository,
            IGenericRepository<TicketArchive> ticketArchiveRepository,
            IGenericRepository<ParkingGate> parkingGateRepository,
            IGenericRepository<User> userRepository,
            IGenericRepository<TicketCollectionTransaction> ticketCollectionTransactionRepository,
            ITokenReader tokenReader,
            IHttpContextAccessor httpContextAccessor,
            IAuditLogger auditLogger,
            ILogger<CollectEnterTicketsCommandHandler> logger)
        {
            _ticketRepository = ticketRepository
                ?? throw new ArgumentNullException(nameof(ticketRepository));

            _ticketArchiveRepository = ticketArchiveRepository
                ?? throw new ArgumentNullException(nameof(ticketArchiveRepository));

            _parkingGateRepository = parkingGateRepository
                ?? throw new ArgumentNullException(nameof(parkingGateRepository));

            _ticketCollectionTransactionRepository = ticketCollectionTransactionRepository
                ?? throw new ArgumentNullException(nameof(ticketCollectionTransactionRepository));

            _tokenReader = tokenReader
                ?? throw new ArgumentNullException(nameof(tokenReader));

            _httpContextAccessor = httpContextAccessor
                ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            _auditLogger = auditLogger
                ?? throw new ArgumentNullException(nameof(auditLogger));

            _logger = logger 
                ?? throw new ArgumentNullException(nameof(logger));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<Result<CollectEnterTicketsResponse>> Handle(
            CollectEnterTicketsCommand request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var tokenInfo = _httpContextAccessor.HttpContext?.ReadToken(_tokenReader);

                if (tokenInfo is null || !tokenInfo.GateId.HasValue || !tokenInfo.UserId.HasValue)
                {
                    _logger.LogWarning(
                        "Token info is missing GateId/UserId while collecting enter tickets.");

                    return Result<CollectEnterTicketsResponse>.Fail(
                        new Error(
                            Code: "Token.GateOrUser.NotFound",
                            Message: ErrorMessage.TokenInfo_Missing,
                            Type: ErrorType.NotFound));
                }

                var gateId = tokenInfo.GateId.Value;
                var supervisorId = tokenInfo.UserId.Value;
                var user = await _userRepository.GetByIdAsync(
    supervisorId,
    cancellationToken);

                if (!string.Equals(tokenInfo.Role, "Supervisor", StringComparison.OrdinalIgnoreCase)  || user is null)
                {
                    return Result<CollectEnterTicketsResponse>.Fail(
                       new Error(
                           Code: "User.NotSupervisor",
                           Message: ErrorMessage.Supervisor_OnlyCanCollectTicketExceedTransactions,
                           Type: ErrorType.Security));
                } 


                var gate = await _parkingGateRepository.GetByIdAsync(
                    gateId,
                    cancellationToken);

                if (gate is null)
                {
                    return Result<CollectEnterTicketsResponse>.Fail(
                        new Error(
                            Code: "Gate.NotFound",
                            Message: ErrorMessage.GateNotFound,
                            Type: ErrorType.NotFound));
                }

                var liveTicketsSpec = new UncollectedEnterTicketsByGateSpec(gateId);
                var archivedTicketsSpec = new UncollectedArchivedEnterTicketsByGateSpec(gateId);

                var liveTickets = await _ticketRepository
                    .ListWithSpecAsync(liveTicketsSpec, cancellationToken);

                var archivedTickets = await _ticketArchiveRepository
                    .ListWithSpecAsync(archivedTicketsSpec, cancellationToken);

                var liveTicketList = liveTickets?.ToList() ?? new List<Ticket>();
                var archivedTicketList = archivedTickets?.ToList() ?? new List<TicketArchive>();

                if (liveTicketList.Count == 0 && archivedTicketList.Count == 0)
                {
                    _logger.LogWarning(
                        "No uncollected enter tickets found for gate {GateId}.",
                        gateId);

                    return Result<CollectEnterTicketsResponse>.Fail(
                        new Error(
                            Code: "Tickets.NoneToCollect",
                            Message: ErrorMessage.Tickets_NoneToCollect,
                            Type: ErrorType.Conflict));
                }

                var collectedAt = DateTime.Now;

                var collectionTransaction = TicketCollectionTransaction.Create(
                    type: TicketCollectionTransactionType.EnterTickets,
                    gateId: gateId,
                    collectedBy: supervisorId,
                    collectedAt: collectedAt);

                foreach (var ticket in liveTicketList)
                {
                    var ticketSnapshot = TicketCollectionTransactionTicket.CreateFromTicket(
                        transactionId: collectionTransaction.Id,
                        ticket: ticket);

                    collectionTransaction.AddTicket(ticketSnapshot);

                    ticket.SetCollected(supervisorId, collectedAt);
                }

                foreach (var ticketArchive in archivedTicketList)
                {
                    var ticketSnapshot = TicketCollectionTransactionTicket.CreateFromTicketArchive(
                        transactionId: collectionTransaction.Id,
                        ticketArchive: ticketArchive);

                    collectionTransaction.AddTicket(ticketSnapshot);

                    ticketArchive.SetCollected(supervisorId, collectedAt);
                }

                collectionTransaction.BuildLinesFromTickets();

                var responsibleUserNames = liveTicketList
                    .Select(t => new
                    {
                        UserId = t.UserId,
                        UserName = t.User?.Name ?? string.Empty
                    })
                    .Concat(
                        archivedTicketList.Select(t => new
                        {
                            UserId = t.UserId,
                            UserName = t.User?.Name ?? string.Empty
                        }))
                    .GroupBy(x => x.UserId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.UserName)
                              .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty);

                var liveTicketDetails = liveTicketList
                    .Select(t => new CollectedEnterTicketDetails
                    {
                        TicketId = t.Id,
                        TicketUniqueCode = t.UniqueCode,
                        Source = TicketSource,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        CollectedAt = t.CollectedDate ?? collectedAt,
                        OriginalPrice = t.Price,
                        VehicleNumber = t.VehicleNumber,
                        CardNumber = t.CardNumber,
                        IsSubscriber = t.IsSubscriber,
                        GateId = t.GateId,
                        ExitGateId = t.ExitGateId
                    });

                var archivedTicketDetails = archivedTicketList
                    .Select(t => new CollectedEnterTicketDetails
                    {
                        TicketId = t.Id,
                        TicketUniqueCode = t.UniqueCode,
                        Source = TicketArchiveSource,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        CollectedAt = t.CollectedDate,
                        OriginalPrice = t.Price,
                        VehicleNumber = t.VehicleNumber,
                        CardNumber = t.CardNumber,
                        IsSubscriber = t.IsSubscriber,
                        GateId = t.GateId,
                        ExitGateId = t.ExitGateId == Guid.Empty ? null : t.ExitGateId
                    });

                var details = liveTicketDetails
                    .Concat(archivedTicketDetails)
                    .OrderByDescending(x => x.CollectedAt)
                    .ThenByDescending(x => x.StartDate)
                    .ToList();

                var response = new CollectEnterTicketsResponse
                {
                    GateId = gateId,
                    CollectedName = user.Name , 
                    CollectedBy = supervisorId,
                    CollectedAt = collectedAt,
                    TotalTicketsCollected = details.Count,
                    TotalOriginalPriceCollected = details.Sum(x => x.OriginalPrice),

                    Lines = collectionTransaction.Lines
                        .Select(x => new CollectedEnterTicketLine
                        {
                            ResponsibleUserId = x.ResponsibleUserId,
                            ResponsibleUserName = responsibleUserNames.TryGetValue(
                                x.ResponsibleUserId,
                                out var responsibleUserName)
                                    ? responsibleUserName
                                    : string.Empty,
                            TicketsCount = x.ItemsCount,
                            TotalOriginalPrice = x.TotalAmount
                        })
                        .OrderByDescending(x => x.TotalOriginalPrice)
                        .ToList(),

                    Tickets = details
                };

                await _ticketCollectionTransactionRepository.AddAsync(
                    collectionTransaction,
                    cancellationToken);

                await _ticketCollectionTransactionRepository.SaveChangesAsync(cancellationToken);

                await SafeAuditAsync(
                    response,
                    collectionTransaction.Id,
                    supervisorId,
                    gateId,
                    cancellationToken);

                return Result<CollectEnterTicketsResponse>.Ok(response);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("CollectEnterTickets operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while collecting enter tickets.");

                return Result<CollectEnterTicketsResponse>.Fail(
                    new Error(
                        Code: "Tickets.CollectEnterTickets.Unexpected",
                        Message: ErrorMessage.Tickets_CollectEnterTicketsUnexpected,
                        Type: ErrorType.Infrastructure));
            }
        }

        private async Task SafeAuditAsync(
            CollectEnterTicketsResponse response,
            Guid transactionId,
            Guid userId,
            Guid gateId,
            CancellationToken cancellationToken)
        {
            try
            {
                await _auditLogger.LogAsync(
                    new AuditLogEntry(
                        EventName: "CollectEnterTicketsSucceeded",
                        EventCategory: "TicketManagement",
                        IsSuccess: true,
                        StatusCode: StatusCodes.Status200OK,
                        UserId: userId,
                        GateId: gateId,
                        Extra: new
                        {
                            TransactionId = transactionId,
                            response.TotalTicketsCollected,
                            response.TotalOriginalPriceCollected,
                            LinesCount = response.Lines.Count,
                            TicketsDetailsCount = response.Tickets.Count,
                            CollectedByUserId = userId,
                            GateId = gateId
                        }),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Audit logging failed for CollectEnterTickets. UserId: {UserId}, GateId: {GateId}, TransactionId: {TransactionId}",
                    userId,
                    gateId,
                    transactionId);
            }
        }
    }
}