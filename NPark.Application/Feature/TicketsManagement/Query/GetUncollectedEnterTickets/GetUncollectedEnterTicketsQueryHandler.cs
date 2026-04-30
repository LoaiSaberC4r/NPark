using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Query.GetUncollectedEnterTickets
{
    internal sealed class GetUncollectedEnterTicketsQueryHandler
        : IQueryHandler<GetUncollectedEnterTicketsQuery, GetUncollectedEnterTicketsResponse>
    {
        private const string TicketSource = "Ticket";
        private const string TicketArchiveSource = "TicketArchive";

        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<TicketArchive> _ticketArchiveRepository;
        private readonly IGenericRepository<ParkingGate> _parkingGateRepository;
        private readonly ITokenReader _tokenReader;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GetUncollectedEnterTicketsQueryHandler> _logger;

        public GetUncollectedEnterTicketsQueryHandler(
            IGenericRepository<Ticket> ticketRepository,
            IGenericRepository<TicketArchive> ticketArchiveRepository,
            IGenericRepository<ParkingGate> parkingGateRepository,
            ITokenReader tokenReader,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GetUncollectedEnterTicketsQueryHandler> logger)
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

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<GetUncollectedEnterTicketsResponse>> Handle(
            GetUncollectedEnterTicketsQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var tokenInfo = _httpContextAccessor.HttpContext?.ReadToken(_tokenReader);

                if (tokenInfo is null || !tokenInfo.GateId.HasValue || !tokenInfo.UserId.HasValue)
                {
                    _logger.LogWarning(
                        "Token info is missing GateId/UserId while getting uncollected enter tickets.");

                    return Result<GetUncollectedEnterTicketsResponse>.Fail(
                        new Error(
                            Code: "Token.GateOrUser.NotFound",
                            Message: ErrorMessage.TokenInfo_Missing,
                            Type: ErrorType.NotFound));
                }

                var gateId = tokenInfo.GateId.Value;

                var gate = await _parkingGateRepository.GetByIdAsync(gateId, cancellationToken);

                if (gate is null)
                {
                    return Result<GetUncollectedEnterTicketsResponse>.Fail(
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

                var liveTicketDetails = (liveTickets ?? Enumerable.Empty<Ticket>())
                    .Select(t => new UncollectedEnterTicketDetails
                    {
                        TicketId = t.Id,
                        TicketUniqueCode = t.UniqueCode,
                        Source = TicketSource,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        OriginalPrice = t.Price,
                        VehicleNumber = t.VehicleNumber,
                        CardNumber = t.CardNumber,
                        IsSubscriber = t.IsSubscriber,
                        GateId = t.GateId,
                        ExitGateId = t.ExitGateId
                    });

                var archivedTicketDetails = (archivedTickets ?? Enumerable.Empty<TicketArchive>())
                    .Select(t => new UncollectedEnterTicketDetails
                    {
                        TicketId = t.Id,
                        TicketUniqueCode = t.UniqueCode,
                        Source = TicketArchiveSource,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        OriginalPrice = t.Price,
                        VehicleNumber = t.VehicleNumber,
                        CardNumber = t.CardNumber,
                        IsSubscriber = t.IsSubscriber,
                        GateId = t.GateId,
                        ExitGateId = t.ExitGateId == Guid.Empty ? null : t.ExitGateId
                    });

                var tickets = liveTicketDetails
                    .Concat(archivedTicketDetails)
                    .OrderByDescending(t => t.StartDate)
                    .ToList();

                var response = new GetUncollectedEnterTicketsResponse
                {
                    GateId = gateId,
                    TotalTicketsCount = tickets.Count,
                    TotalOriginalPrice = tickets.Sum(t => t.OriginalPrice),
                    Tickets = tickets
                };

                return Result<GetUncollectedEnterTicketsResponse>.Ok(response);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("GetUncollectedEnterTickets operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while getting uncollected enter tickets.");

                return Result<GetUncollectedEnterTicketsResponse>.Fail(
                    new Error(
                        Code: "Tickets.GetUncollectedEnterTickets.Unexpected",
                        Message: ErrorMessage.Tickets_GetUncollectedEnterTicketsUnexpected,
                        Type: ErrorType.Infrastructure));
            }
        }
    }
}