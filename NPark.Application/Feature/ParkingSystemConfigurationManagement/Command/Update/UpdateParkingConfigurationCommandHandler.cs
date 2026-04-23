using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Application.Shared.Dto;
using NPark.Application.Specifications.ParkingGateSpec;
using NPark.Application.Specifications.ParkingSystemConfigurationSpec;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.FileNames;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.ParkingSystemConfigurationManagement.Command.Update
{
    public sealed class UpdateParkingConfigurationCommandHandler
        : ICommandHandler<UpdateParkingConfigurationCommand>
    {
        private readonly IGenericRepository<ParkingSystemConfiguration> _parkingSystemConfigurationRepository;
        private readonly IGenericRepository<ParkingGate> _parkingGateRepository;
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateParkingConfigurationCommandHandler> _logger;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<ONVIFCameraSetting> _onvifCameraSettingRepository;

        public UpdateParkingConfigurationCommandHandler(
            IGenericRepository<ParkingSystemConfiguration> parkingSystemConfigurationRepository,
            IGenericRepository<ParkingGate> parkingGateRepository,
            IGenericRepository<Ticket> ticketRepository,
            IAuditLogger auditLogger,
            ILogger<UpdateParkingConfigurationCommandHandler> logger,
            IGenericRepository<User> userRepository,
            IGenericRepository<ONVIFCameraSetting> onvifCameraSettingRepository)
        {
            _parkingSystemConfigurationRepository = parkingSystemConfigurationRepository
                ?? throw new ArgumentNullException(nameof(parkingSystemConfigurationRepository));
            _parkingGateRepository = parkingGateRepository
                ?? throw new ArgumentNullException(nameof(parkingGateRepository));
            _ticketRepository = ticketRepository
                ?? throw new ArgumentNullException(nameof(ticketRepository));
            _auditLogger = auditLogger
                ?? throw new ArgumentNullException(nameof(auditLogger));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));
            _onvifCameraSettingRepository = onvifCameraSettingRepository
                ?? throw new ArgumentNullException(nameof(onvifCameraSettingRepository));
        }

        public async Task<Result> Handle(
            UpdateParkingConfigurationCommand request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var spec = new GetParkingSystemConfigurationForUpdateSpecification();
                var configuration = await _parkingSystemConfigurationRepository
                    .FirstOrDefaultWithSpecAsync(spec, cancellationToken);

                if (configuration is null)
                {
                    return await CreateConfigurationAsync(request, cancellationToken);
                }

                return await UpdateConfigurationAsync(configuration, request, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("UpdateParkingConfiguration operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating parking configuration.");

                return Result.Fail(
                    new Error(
                        Code: "ParkingConfiguration.Update.Unexpected",
                        Message: ErrorMessage.ParkingConfigurationUpdate_Unexpected,
                        Type: ErrorType.Infrastructure));
            }
        }

        private async Task<Result> CreateConfigurationAsync(
            UpdateParkingConfigurationCommand request,
            CancellationToken cancellationToken)
        {
            var pricingSchemaId =
                request.PricingSchemaId == Guid.Empty ? null : request.PricingSchemaId;

            var gracePeriod = request.gracePeriodMinutes.HasValue
                ? TimeSpan.FromMinutes(request.gracePeriodMinutes.Value)
                : (TimeSpan?)null;

            var configuration = ParkingSystemConfiguration.Create(
                entryGatesCount: request.EntryGatesCount,
                exitGatesCount: request.ExitGatesCount,
                allowedParkingSlots: request.AllowedParkingSlots,
                priceType: request.PriceType,
                vehiclePassengerData: request.VehiclePassengerData,
                printType: request.PrintType,
                dateTimeFlag: request.DateTimeFlag,
                ticketIdFlag: request.TicketIdFlag,
                feesFlag: request.FeesFlag,
                pricingSchemaId: pricingSchemaId,
                gracePeriod: gracePeriod);

            foreach (var gateInfo in request.EntryGates)
            {
                var validationResult = await ValidateGateInfoAsync(
                    gateInfo,
                    GateType.Entrance,
                    cancellationToken);

                if (validationResult.IsFailure)
                    return validationResult;

                var onvifCameraSettingId = await CreateOrUpdateOnvifCameraSettingAsync(
                    gateInfo.ONVIFCameraSetting,
                    existingCameraSettingId: null,
                    cancellationToken);

                var parkingGate = ParkingGate.Create(
                    gateNumber: gateInfo.GateNumber,
                    gateType: GateType.Entrance,
                    lprIp: gateInfo.LprIp,
                    pcIp: gateInfo.PcIp,
                    controlIp: gateInfo.ControlIp,
                    onvifCameraSettingId: onvifCameraSettingId);

                parkingGate.UpdateGateInfo(
                    gateNumber: gateInfo.GateNumber,
                    gateType: GateType.Entrance,
                    lprIp: gateInfo.LprIp,
                    pcIp: gateInfo.PcIp,
                    controlIp: gateInfo.ControlIp,
                    onvifCameraSettingId: onvifCameraSettingId,
                    assignedTo: gateInfo.IsAssigned ? gateInfo.AssignedTo : null,
                    isAssigned: gateInfo.IsAssigned);

                configuration.AddParkingGate(parkingGate);
            }

            foreach (var gateInfo in request.ExitGates)
            {
                var validationResult = await ValidateGateInfoAsync(
                    gateInfo,
                    GateType.Exit,
                    cancellationToken);

                if (validationResult.IsFailure)
                    return validationResult;

                var onvifCameraSettingId = await CreateOrUpdateOnvifCameraSettingAsync(
                    gateInfo.ONVIFCameraSetting,
                    existingCameraSettingId: null,
                    cancellationToken);

                var parkingGate = ParkingGate.Create(
                    gateNumber: gateInfo.GateNumber,
                    gateType: GateType.Exit,
                    lprIp: gateInfo.LprIp,
                    pcIp: gateInfo.PcIp,
                    controlIp: gateInfo.ControlIp,
                    onvifCameraSettingId: onvifCameraSettingId);

                parkingGate.UpdateGateInfo(
                    gateNumber: gateInfo.GateNumber,
                    gateType: GateType.Exit,
                    lprIp: gateInfo.LprIp,
                    pcIp: gateInfo.PcIp,
                    controlIp: gateInfo.ControlIp,
                    onvifCameraSettingId: onvifCameraSettingId,
                    assignedTo: gateInfo.IsAssigned ? gateInfo.AssignedTo : null,
                    isAssigned: gateInfo.IsAssigned);

                configuration.AddParkingGate(parkingGate);
            }

            await _parkingSystemConfigurationRepository.AddAsync(configuration, cancellationToken);
            await _parkingSystemConfigurationRepository.SaveChangesAsync(cancellationToken);

            await SafeAuditAsync(
                eventName: "CreateParkingSystemConfiguration",
                statusCode: 201,
                request: request,
                configurationId: configuration.Id,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Parking system configuration created successfully. ConfigurationId: {ConfigurationId}",
                configuration.Id);

            return Result.Ok();
        }

        private async Task<Result> UpdateConfigurationAsync(
            ParkingSystemConfiguration configuration,
            UpdateParkingConfigurationCommand request,
            CancellationToken cancellationToken)
        {
            var pricingSchemaId =
                request.PricingSchemaId == Guid.Empty ? null : request.PricingSchemaId;

            var gracePeriod = request.gracePeriodMinutes.HasValue
                ? TimeSpan.FromMinutes(request.gracePeriodMinutes.Value)
                : (TimeSpan?)null;

            configuration.Update(
                configuration,
                request.EntryGatesCount,
                request.ExitGatesCount,
                request.AllowedParkingSlots,
                request.PriceType,
                request.VehiclePassengerData,
                request.PrintType,
                request.DateTimeFlag,
                request.TicketIdFlag,
                request.FeesFlag,
                pricingSchemaId,
                gracePeriod);

            await _parkingSystemConfigurationRepository.SaveChangesAsync(cancellationToken);

            var gateSpec = new GetParkingGateBySystemConfiguration(configuration.Id);
            var existingGates = (await _parkingGateRepository
                .ListWithSpecAsync(gateSpec, cancellationToken))
                .ToList();

            var existingGateById = existingGates
                .Where(g => g.Id != Guid.Empty)
                .ToDictionary(g => g.Id, g => g);

            var requestedEntryGates = request.EntryGates;
            var requestedExitGates = request.ExitGates;

            var requestedGateIds = request.EntryGates
                .Concat(request.ExitGates)
                .Where(g => g.GateId.HasValue)
                .Select(g => g.GateId!.Value)
                .ToHashSet();

            var updateEntryResult = await UpdateExistingGatesAsync(
                existingGateById,
                requestedEntryGates,
                GateType.Entrance,
                cancellationToken);

            if (updateEntryResult.IsFailure)
                return updateEntryResult;

            var updateExitResult = await UpdateExistingGatesAsync(
                existingGateById,
                requestedExitGates,
                GateType.Exit,
                cancellationToken);

            if (updateExitResult.IsFailure)
                return updateExitResult;

            await _parkingGateRepository.SaveChangesAsync(cancellationToken);

            var gatesToDelete = existingGates
                .Where(g => !requestedGateIds.Contains(g.Id))
                .ToList();

            if (gatesToDelete.Count > 0)
            {
                foreach (var gate in gatesToDelete)
                {
                    var hasTickets = await _ticketRepository
                        .IsExistAsync(t => t.GateId == gate.Id, cancellationToken);

                    if (hasTickets)
                    {
                        _logger.LogWarning(
                            "Cannot delete gate with tickets. GateId: {GateId}, GateNumber: {GateNumber}",
                            gate.Id,
                            gate.GateNumber);

                        return Result.Fail(
                            new Error(
                                Code: "Gate.HasTickets",
                                Message: ErrorMessage.Gate_HasTickets,
                                Type: ErrorType.Conflict));
                    }
                }

                _parkingGateRepository.DeleteRange(gatesToDelete);
                await _parkingGateRepository.SaveChangesAsync(cancellationToken);
            }

            var newGates = new List<ParkingGate>();

            foreach (var gateInfo in requestedEntryGates.Where(g => !g.GateId.HasValue))
            {
                var validationResult = await ValidateGateInfoAsync(
                    gateInfo,
                    GateType.Entrance,
                    cancellationToken);

                if (validationResult.IsFailure)
                    return validationResult;

                var onvifCameraSettingId = await CreateOrUpdateOnvifCameraSettingAsync(
                    gateInfo.ONVIFCameraSetting,
                    existingCameraSettingId: null,
                    cancellationToken);

                var parkingGate = ParkingGate.Create(
                    gateNumber: gateInfo.GateNumber,
                    gateType: GateType.Entrance,
                    lprIp: gateInfo.LprIp,
                    pcIp: gateInfo.PcIp,
                    controlIp: gateInfo.ControlIp,
                    onvifCameraSettingId: onvifCameraSettingId,
                    id: configuration.Id);

                parkingGate.UpdateGateInfo(
                    gateNumber: gateInfo.GateNumber,
                    gateType: GateType.Entrance,
                    lprIp: gateInfo.LprIp,
                    pcIp: gateInfo.PcIp,
                    controlIp: gateInfo.ControlIp,
                    onvifCameraSettingId: onvifCameraSettingId,
                    assignedTo: gateInfo.IsAssigned ? gateInfo.AssignedTo : null,
                    isAssigned: gateInfo.IsAssigned);

                newGates.Add(parkingGate);
            }

            foreach (var gateInfo in requestedExitGates.Where(g => !g.GateId.HasValue))
            {
                var validationResult = await ValidateGateInfoAsync(
                    gateInfo,
                    GateType.Exit,
                    cancellationToken);

                if (validationResult.IsFailure)
                    return validationResult;

                var onvifCameraSettingId = await CreateOrUpdateOnvifCameraSettingAsync(
                    gateInfo.ONVIFCameraSetting,
                    existingCameraSettingId: null,
                    cancellationToken);

                var parkingGate = ParkingGate.Create(
                    gateNumber: gateInfo.GateNumber,
                    gateType: GateType.Exit,
                    lprIp: gateInfo.LprIp,
                    pcIp: gateInfo.PcIp,
                    controlIp: gateInfo.ControlIp,
                    onvifCameraSettingId: onvifCameraSettingId,
                    id: configuration.Id);

                parkingGate.UpdateGateInfo(
                    gateNumber: gateInfo.GateNumber,
                    gateType: GateType.Exit,
                    lprIp: gateInfo.LprIp,
                    pcIp: gateInfo.PcIp,
                    controlIp: gateInfo.ControlIp,
                    onvifCameraSettingId: onvifCameraSettingId,
                    assignedTo: gateInfo.IsAssigned ? gateInfo.AssignedTo : null,
                    isAssigned: gateInfo.IsAssigned);

                newGates.Add(parkingGate);
            }

            if (newGates.Count > 0)
            {
                await _parkingGateRepository.AddRangeAsync(newGates, cancellationToken);
                await _parkingGateRepository.SaveChangesAsync(cancellationToken);
            }

            await SafeAuditAsync(
                eventName: "UpdateParkingSystemConfiguration",
                statusCode: 200,
                request: request,
                configurationId: configuration.Id,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Parking system configuration updated successfully. ConfigurationId: {ConfigurationId}",
                configuration.Id);

            return Result.Ok();
        }

        private async Task<Result> UpdateExistingGatesAsync(
            IDictionary<Guid, ParkingGate> existingGateById,
            IEnumerable<GateInfo> gateInfos,
            GateType gateType,
            CancellationToken cancellationToken)
        {
            foreach (var gateInfo in gateInfos.Where(g => g.GateId.HasValue))
            {
                if (!existingGateById.TryGetValue(gateInfo.GateId!.Value, out var gate))
                {
                    continue;
                }

                var validationResult = await ValidateGateInfoAsync(
                    gateInfo,
                    gateType,
                    cancellationToken);

                if (validationResult.IsFailure)
                    return validationResult;

                var onvifCameraSettingId = await CreateOrUpdateOnvifCameraSettingAsync(
                    gateInfo.ONVIFCameraSetting,
                    gate.ONVIFCameraSettingId,
                    cancellationToken);

                gate.UpdateGateInfo(
                    gateNumber: gateInfo.GateNumber,
                    gateType: gateType,
                    lprIp: gateInfo.LprIp,
                    pcIp: gateInfo.PcIp,
                    controlIp: gateInfo.ControlIp,
                    onvifCameraSettingId: onvifCameraSettingId,
                    assignedTo: gateInfo.IsAssigned ? gateInfo.AssignedTo : null,
                    isAssigned: gateInfo.IsAssigned);
            }

            return Result.Ok();
        }

        private async Task<Result> ValidateGateInfoAsync(
            GateInfo gateInfo,
            GateType gateType,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(gateInfo.ControlIp))
            {
                return Result.Fail(
                    new Error(
                        Code: "Gate.ControlIp.Required",
                        Message: $"Gate {gateInfo.GateNumber} must have a control IP.",
                        Type: ErrorType.Validation));
            }

            if (gateInfo.ONVIFCameraSetting is not null)
            {
                if (string.IsNullOrWhiteSpace(gateInfo.ONVIFCameraSetting.UrlRequest))
                {
                    return Result.Fail(
                        new Error(
                            Code: "Gate.ONVIF.UrlRequest.Required",
                            Message: $"Gate {gateInfo.GateNumber} ONVIF UrlRequest is required.",
                            Type: ErrorType.Validation));
                }

                if (string.IsNullOrWhiteSpace(gateInfo.ONVIFCameraSetting.UserName))
                {
                    return Result.Fail(
                        new Error(
                            Code: "Gate.ONVIF.UserName.Required",
                            Message: $"Gate {gateInfo.GateNumber} ONVIF UserName is required.",
                            Type: ErrorType.Validation));
                }

                if (string.IsNullOrWhiteSpace(gateInfo.ONVIFCameraSetting.Password))
                {
                    return Result.Fail(
                        new Error(
                            Code: "Gate.ONVIF.Password.Required",
                            Message: $"Gate {gateInfo.GateNumber} ONVIF Password is required.",
                            Type: ErrorType.Validation));
                }
            }

            if (!gateInfo.IsAssigned)
                return Result.Ok();

            if (gateInfo.AssignedTo is null)
            {
                return Result.Fail(
                    new Error(
                        Code: "Gate.AssignedTo.Required",
                        Message: $"Gate {gateInfo.GateNumber} must have an assigned user.",
                        Type: ErrorType.Validation));
            }

            var assignedUser = await _userRepository.GetByIdAsync(gateInfo.AssignedTo.Value, cancellationToken);

            var expectedRoleId = gateType == GateType.Entrance
                ? SystemRoles.EntranceCashierRoleId
                : SystemRoles.ExitCashierRoleId;

            if (assignedUser is null || assignedUser.RoleId != expectedRoleId)
            {
                return Result.Fail(
                    new Error(
                        Code: "Gate.AssignedToInvalidUser",
                        Message: $"Gate {gateInfo.GateNumber} is assigned to an invalid user.",
                        Type: ErrorType.Validation));
            }

            return Result.Ok();
        }

        private async Task<Guid?> CreateOrUpdateOnvifCameraSettingAsync(
            ONVIFCameraSettingDto? dto,
            Guid? existingCameraSettingId,
            CancellationToken cancellationToken)
        {
            if (dto is null)
            {
                return null;
            }

            if (existingCameraSettingId.HasValue)
            {
                var existing = await _onvifCameraSettingRepository
                    .GetByIdAsync(existingCameraSettingId.Value, cancellationToken);

                if (existing is not null)
                {
                    existing.Update(dto.UrlRequest, dto.UserName, dto.Password);
                    await _onvifCameraSettingRepository.SaveChangesAsync(cancellationToken);
                    return existing.Id;
                }
            }

            var cameraSetting = ONVIFCameraSetting.Create(
                dto.UrlRequest,
                dto.UserName,
                dto.Password);

            await _onvifCameraSettingRepository.AddAsync(cameraSetting, cancellationToken);
            await _onvifCameraSettingRepository.SaveChangesAsync(cancellationToken);

            return cameraSetting.Id;
        }

        private async Task SafeAuditAsync(
            string eventName,
            int statusCode,
            UpdateParkingConfigurationCommand request,
            int configurationId,
            CancellationToken cancellationToken)
        {
            try
            {
                await _auditLogger.LogAsync(
                    new AuditLogEntry(
                        EventName: eventName,
                        EventCategory: "ParkingSystem",
                        IsSuccess: true,
                        StatusCode: statusCode,
                        UserId: null,
                        GateId: null,
                        Extra: new
                        {
                            ConfigurationId = configurationId,
                            request.EntryGatesCount,
                            request.ExitGatesCount,
                            request.AllowedParkingSlots,
                            request.PriceType,
                            request.gracePeriodMinutes,
                            TotalEntryGates = request.EntryGates.Count,
                            TotalExitGates = request.ExitGates.Count,
                            LoggedAt = DateTime.UtcNow
                        }),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Audit logging failed for {EventName}. ConfigurationId: {ConfigurationId}",
                    eventName,
                    configurationId);
            }
        }
    }
}