using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Application.Shared.Dto;
using NPark.Application.Specifications.GateSpecification;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.ExitTicketFromMqtt.ExitTicketFromMqttCommand
{
    internal sealed class ExitTicketFromMqttCommandHandler
        : ICommandHandler<ExitTicketFromMqttCommand, ExitTicketFromMqttCommandResponse>
    {
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<TicketArchive> _ticketArchiveRepository;
        private readonly IGenericRepository<ParkingGate> _parkingGateRepository;
        private readonly IRealtimeNotifier _realtimeNotifier;
        private readonly ILogger<ExitTicketFromMqttCommandHandler> _logger;

        public ExitTicketFromMqttCommandHandler(
            IGenericRepository<Ticket> ticketRepository,
            IGenericRepository<TicketArchive> ticketArchiveRepository,
            IGenericRepository<ParkingGate> parkingGateRepository,
            IRealtimeNotifier realtimeNotifier,
            ILogger<ExitTicketFromMqttCommandHandler> logger)
        {
            _ticketRepository = ticketRepository
                ?? throw new ArgumentNullException(nameof(ticketRepository));

            _ticketArchiveRepository = ticketArchiveRepository
                ?? throw new ArgumentNullException(nameof(ticketArchiveRepository));

            _parkingGateRepository = parkingGateRepository
                ?? throw new ArgumentNullException(nameof(parkingGateRepository));

            _realtimeNotifier = realtimeNotifier
                ?? throw new ArgumentNullException(nameof(realtimeNotifier));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ExitTicketFromMqttCommandResponse>> Handle(
            ExitTicketFromMqttCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "ExitTicketFromMqtt started. TicketId={TicketId}, ControllerId={ControllerId}",
                request.TicketId,
                request.ControllerId);

            var controllerIdText = request.ControllerId.Trim();

            if (!int.TryParse(controllerIdText, out var controllerId))
            {
                _logger.LogWarning(
                    "ExitTicketFromMqtt stopped. ControllerId is invalid. ControllerId={ControllerId}",
                    controllerIdText);

                return Result<ExitTicketFromMqttCommandResponse>.Fail(
                    new Error(
                        Code: "MqttExit.ControllerId.Invalid",
                        Message: "Controller id must be numeric.",
                        Type: ErrorType.Validation));
            }

            _logger.LogInformation(
                "Resolving exit gate by ControllerId={ControllerId}",
                controllerId);

            var gateSpec = new ParkingGateByControllerIdSpecification(controllerId);

            var gateInfo = await _parkingGateRepository.FirstOrDefaultWithSpecAsync(
                gateSpec,
                cancellationToken);

            if (gateInfo is null)
            {
                _logger.LogWarning(
                    "ExitTicketFromMqtt stopped. Gate not found. ControllerId={ControllerId}",
                    controllerId);

                return Result<ExitTicketFromMqttCommandResponse>.Fail(
                    new Error(
                        Code: "Gate.NotFound",
                        Message: ErrorMessage.GateNotFound,
                        Type: ErrorType.NotFound));
            }

            _logger.LogInformation(
                "Exit gate resolved. ControllerId={ControllerId}, GateId={GateId}, GateNumber={GateNumber}, GateType={GateType}",
                controllerId,
                gateInfo.Id,
                gateInfo.GateNumber,
                gateInfo.GateType);

            var ticketEntity = await _ticketRepository.GetByIdAsync(request.TicketId);

            _logger.LogInformation(
                "Ticket loaded for MQTT exit. TicketId={TicketId}, Found={Found}",
                request.TicketId,
                ticketEntity is not null);

            if (ticketEntity is null)
            {
                _logger.LogWarning(
                    "ExitTicketFromMqtt stopped. Ticket not found. TicketId={TicketId}",
                    request.TicketId);

                return Result<ExitTicketFromMqttCommandResponse>.Fail(
                    new Error(
                        Code: "Ticket.NotFound",
                        Message: ErrorMessage.Ticket_NotFound,
                        Type: ErrorType.NotFound));
            }

            if (!ticketEntity.IsCashierCollected)
            {
                _logger.LogWarning(
                    "ExitTicketFromMqtt stopped. Ticket not collected. TicketId={TicketId}, IsCashierCollected={IsCashierCollected}",
                    ticketEntity.Id,
                    ticketEntity.IsCashierCollected);

                return Result<ExitTicketFromMqttCommandResponse>.Fail(
                    new Error(
                        Code: "Ticket.NotCollected",
                        Message: ErrorMessage.Ticket_CashierCollected_Required,
                        Type: ErrorType.Conflict));
            }

            if (ticketEntity.EndDate.HasValue)
            {
                _logger.LogWarning(
                    "ExitTicketFromMqtt stopped. Ticket already exited. TicketId={TicketId}, EndDate={EndDate}",
                    ticketEntity.Id,
                    ticketEntity.EndDate);

                return Result<ExitTicketFromMqttCommandResponse>.Fail(
                    new Error(
                        Code: "Ticket.AlreadyExited",
                        Message: ErrorMessage.TicketAlreadyExited,
                        Type: ErrorType.Conflict));
            }

            var now = DateTime.Now;

            if (ticketEntity.StartDate.Date != now.Date)
            {
                _logger.LogWarning(
                    "ExitTicketFromMqtt stopped. Ticket is not valid for today. TicketId={TicketId}, StartDate={StartDate}, Now={Now}",
                    ticketEntity.Id,
                    ticketEntity.StartDate,
                    now);

                return Result<ExitTicketFromMqttCommandResponse>.Fail(
                    new Error(
                        Code: "Ticket.NotValidForExit",
                        Message: "Ticket is not valid for exit today.",
                        Type: ErrorType.Conflict));
            }

            _logger.LogInformation(
                "Setting ticket exit data. TicketId={TicketId}, GateId={GateId}",
                ticketEntity.Id,
                gateInfo.Id);

            ticketEntity.SetExitGate(gateInfo.Id);
            ticketEntity.SetExitDate();

            await _ticketRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Ticket exit data saved. TicketId={TicketId}, ExitDate={ExitDate}",
                ticketEntity.Id,
                ticketEntity.EndDate);

            await NotifyDashboardTicketExitedAsync(
                ticketEntity,
                gateInfo,
                request.UserId,
                cancellationToken);

            var ticketArchive = TicketArchive.CreateFromTicket(ticketEntity);

            await _ticketArchiveRepository.AddAsync(ticketArchive, cancellationToken);
            await _ticketArchiveRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Ticket archived from MQTT exit. TicketId={TicketId}",
                ticketEntity.Id);

            _ticketRepository.Delete(ticketEntity);
            await _ticketRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Original ticket deleted after archive. TicketId={TicketId}",
                ticketEntity.Id);

            var response = new ExitTicketFromMqttCommandResponse
            {
                TicketId = ticketEntity.Id,
                GateId = gateInfo.Id,
                ControllerId = controllerIdText,
                UserId = request.UserId,
                ExitDate = ticketEntity.EndDate ?? now,
                TicketExited = true,
                Archived = true
            };

            _logger.LogInformation(
                "ExitTicketFromMqtt completed. TicketId={TicketId}, GateId={GateId}, ExitDate={ExitDate}, Archived=true",
                response.TicketId,
                response.GateId,
                response.ExitDate);

            return Result<ExitTicketFromMqttCommandResponse>.Ok(response);
        }

        private async Task NotifyDashboardTicketExitedAsync(
            Ticket ticketEntity,
            ParkingGate gateInfo,
            Guid? userId,
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
                    GateId = gateInfo.Id,

                    // MQTT background flow has no logged-in HTTP user.
                    UserId = userId ?? Guid.Empty,

                    ExitDate = ticketEntity.EndDate ?? DateTime.Now,
                    GateNumber = gateNumber
                };

                await _realtimeNotifier.NotifyTicketExitedAsync(payload);

                _logger.LogInformation(
                    "Realtime ticket exited notification sent. TicketId={TicketId}, GateId={GateId}",
                    ticketEntity.Id,
                    gateInfo.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send realtime MQTT exit notification for ticket {TicketId}",
                    ticketEntity.Id);
            }
        }
    }
}