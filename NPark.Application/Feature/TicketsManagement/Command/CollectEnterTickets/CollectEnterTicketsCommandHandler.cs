using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.CollectEnterTickets
{
    internal sealed class CollectEnterTicketsCommandHandler
         : ICommandHandler<CollectEnterTicketsCommand, CollectEnterTicketsResponse>
    {
        private const string TicketSource = "Ticket";
        private const string TicketArchiveSource = "TicketArchive";

        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<TicketArchive> _ticketArchiveRepository;
        private readonly IGenericRepository<ParkingGate> _parkingGateRepository;
        private readonly ITokenReader _tokenReader;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CollectEnterTicketsCommandHandler> _logger;

        public CollectEnterTicketsCommandHandler(
            IGenericRepository<Ticket> ticketRepository,
            IGenericRepository<TicketArchive> ticketArchiveRepository,
            IGenericRepository<ParkingGate> parkingGateRepository,
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

            _tokenReader = tokenReader
                ?? throw new ArgumentNullException(nameof(tokenReader));

            _httpContextAccessor = httpContextAccessor
                ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            _auditLogger = auditLogger
                ?? throw new ArgumentNullException(nameof(auditLogger));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
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
                var userId = tokenInfo.UserId.Value;
                if (tokenInfo.Role != "Supervisor")
                {
                    return Result<CollectEnterTicketsResponse>.Fail(
                       new Error(
                           Code: "User.NotSupervisor",
                           Message: ErrorMessage.Supervisor_OnlyCanCollectTicketExceedTransactions,
                           Type: ErrorType.Security));
                }

                var gate = await _parkingGateRepository.GetByIdAsync(gateId, cancellationToken);

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

                foreach (var ticket in liveTicketList)
                {
                    ticket.SetCollected(userId);
                }

                foreach (var ticket in archivedTicketList)
                {
                    ticket.SetCollected(userId);
                }

                var fallbackCollectedAt = DateTime.Now;

                var liveTicketDetails = liveTicketList
                    .Select(t => new CollectedEnterTicketDetails
                    {
                        TicketId = t.Id,
                        TicketUniqueCode = t.UniqueCode,
                        Source = TicketSource,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        CollectedAt = t.CollectedDate ?? fallbackCollectedAt,
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
                    CollectedBy = userId,
                    CollectedAt = fallbackCollectedAt,
                    TotalTicketsCollected = details.Count,
                    TotalOriginalPriceCollected = details.Sum(x => x.OriginalPrice),
                    Tickets = details
                };

                await _ticketRepository.SaveChangesAsync(cancellationToken);
                await _ticketArchiveRepository.SaveChangesAsync(cancellationToken);

                await SafeAuditAsync(
                    response,
                    userId,
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
                        Message: $"{ex}" + ErrorMessage.Tickets_CollectEnterTicketsUnexpected,
                        Type: ErrorType.Infrastructure));
            }
        }

        private async Task SafeAuditAsync(
            CollectEnterTicketsResponse response,
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
                            response.TotalTicketsCollected,
                            response.TotalOriginalPriceCollected,
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
                    "Audit logging failed for CollectEnterTickets. UserId: {UserId}, GateId: {GateId}",
                    userId,
                    gateId);
            }
        }
    }
}