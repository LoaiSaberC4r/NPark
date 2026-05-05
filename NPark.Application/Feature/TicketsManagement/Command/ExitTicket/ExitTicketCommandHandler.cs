using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Application.Abstraction.Security;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.ExitTicket
{
    public sealed class ExitTicketCommandHandler : ICommandHandler<ExitTicketCommand>
    {
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<TicketArchive> _ticketArchiveRepository;
        private readonly IGenericRepository<ParkingGate> _parkingGateRepository;
        private readonly ITokenReader _tokenReader;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRealtimeNotifier _realtimeNotifier;
        private readonly ILogger<ExitTicketCommandHandler> _logger;

        public ExitTicketCommandHandler(
            IGenericRepository<Ticket> ticketRepository,
            IHttpContextAccessor httpContextAccessor,
            ITokenReader tokenReader,
            IGenericRepository<TicketArchive> ticketArchiveRepository,
            IRealtimeNotifier realTimeNotifier,
            ILogger<ExitTicketCommandHandler> logger,
            IGenericRepository<ParkingGate> parkingGateRepository)
        {
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _tokenReader = tokenReader ?? throw new ArgumentNullException(nameof(tokenReader));
            _ticketArchiveRepository = ticketArchiveRepository ?? throw new ArgumentNullException(nameof(ticketArchiveRepository));
            _realtimeNotifier = realTimeNotifier ?? throw new ArgumentNullException(nameof(realTimeNotifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parkingGateRepository = parkingGateRepository ?? throw new ArgumentNullException(nameof(parkingGateRepository));
        }

        public async Task<Result> Handle(ExitTicketCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tokenInfo = _httpContextAccessor.HttpContext?.ReadToken(_tokenReader);

            if (tokenInfo is null || !tokenInfo.GateId.HasValue || !tokenInfo.UserId.HasValue)
            {
                return Result.Fail(
                    new Error(
                        Code: "Token.GateOrUser.NotFound",
                        Message: ErrorMessage.TokenInfo_Missing,
                        Type: ErrorType.NotFound));
            }

            var gateId = tokenInfo.GateId.Value;
            var userId = tokenInfo.UserId.Value;

            var gateInfo = await _parkingGateRepository.GetByIdAsync(gateId, cancellationToken);

            if (gateInfo is null)
            {
                _logger.LogWarning(
                    "Gate info not found while exiting ticket. GateId={GateId}",
                    gateId);

                return Result.Fail(
                    new Error(
                        Code: "Gate.NotFound",
                        Message: ErrorMessage.GateNotFound,
                        Type: ErrorType.NotFound));
            }

            var ticketSpec = new GetTicketForExitArchiveSpec(request.TicketId);

            var ticketEntity = await _ticketRepository
                .FirstOrDefaultWithSpecAsync(ticketSpec, cancellationToken);

            if (ticketEntity is null)
            {
                return Result.Fail(
                    new Error(
                        Code: "Ticket.NotFound",
                        Message: ErrorMessage.Ticket_NotFound,
                        Type: ErrorType.NotFound));
            }

            if (!ticketEntity.IsCashierCollected)
            {
                return Result.Fail(
                    new Error(
                        Code: "Ticket.NotCollected",
                        Message: ErrorMessage.Ticket_CashierCollected_Required,
                        Type: ErrorType.Conflict));
            }

            if (ticketEntity.EndDate.HasValue)
            {
                return Result.Fail(
                    new Error(
                        Code: "Ticket.AlreadyExited",
                        Message: ErrorMessage.TicketAlreadyExited,
                        Type: ErrorType.Conflict));
            }

            ticketEntity.SetExitGate(gateId);
            ticketEntity.SetExitDate();

            /*
             * هنا بنعمل Snapshot قبل الحذف.
             * لازم TicketAttachments تكون Loaded من الـ Specification.
             */
            var ticketArchive = TicketArchive.CreateFromTicket(ticketEntity);

            await _ticketArchiveRepository.AddAsync(ticketArchive, cancellationToken);

            _ticketRepository.Delete(ticketEntity);

            await _ticketRepository.SaveChangesAsync(cancellationToken);
            await _ticketArchiveRepository.SaveChangesAsync(cancellationToken);

            await NotifyDashboardTicketExitedAsync(
                ticketEntity,
                gateId,
                userId,
                gateInfo,
                cancellationToken);

            return Result.Ok();
        }

        private async Task NotifyDashboardTicketExitedAsync(
            Ticket ticketEntity,
            Guid gateId,
            Guid userId,
            ParkingGate gateInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                var gateNumber = gateInfo.GateType == GateType.Entrance
                    ? $"{ErrorMessage.EntranceGate} {gateInfo.GateNumber}"
                    : $"{ErrorMessage.ExitGate} {gateInfo.GateNumber}";

                var payload = new TicketExitedNotification
                {
                    TicketId = ticketEntity.Id,
                    GateId = gateId,
                    UserId = userId,
                    ExitDate = ticketEntity.EndDate ?? DateTime.Now,
                    GateNumber = gateNumber
                };

                await _realtimeNotifier.NotifyTicketExitedAsync(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send realtime exit notification for ticket {TicketId}",
                    ticketEntity.Id);
            }
        }
    }
}