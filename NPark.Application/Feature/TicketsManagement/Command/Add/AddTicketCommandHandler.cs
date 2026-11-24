using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NPark.Application.Services;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.Media;
using BuildingBlock.Application.Abstraction.QrCode;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Application.Abstraction.Security;
using NPark.Application.Shared.Dto;
using NPark.Application.Specifications.ParkingMembershipSpecification;
using NPark.Application.Specifications.ParkingSystemConfigurationSpec;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.FileNames;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.Add
{
    public sealed class AddTicketCommandHandler
         : ICommandHandler<AddTicketCommand, AddTicketCommandResponse>
    {
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<ParkingGate> _parkingGateRepository;
        private readonly IGenericRepository<PricingScheme> _pricingSchemaRepository;
        private readonly IGenericRepository<ParkingSystemConfiguration> _parkingSystemConfigurationRepository;
        private readonly IGenericRepository<ParkingMemberships> _parkingMembershipsRepository;
        private readonly IQRCodeService _qrCodeService;
        private readonly IByteVerificationService _byteVerificationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenReader _tokenReader;
        private readonly IMediaService _mediaService;
        private readonly ILogger<AddTicketCommandHandler> _logger;
        private readonly IRealtimeNotifier _realtimeNotifier;
        private readonly ParkingBusinessClock _clock;
        private readonly ISender _sender;
        private readonly IServiceScopeFactory _scopes;

        public AddTicketCommandHandler(
            IGenericRepository<Ticket> ticketRepository,
            IGenericRepository<ParkingMemberships> parkingMembershipsRepository,
            IQRCodeService qrCodeService,
            IHttpContextAccessor httpContextAccessor,
            IByteVerificationService byteVerificationService,
            ITokenReader tokenReader,
            IGenericRepository<PricingScheme> pricingSchemaRepository,
            IMediaService mediaService,
            ILogger<AddTicketCommandHandler> logger,
            IRealtimeNotifier realtimeNotifier,
            IGenericRepository<ParkingSystemConfiguration> parkingSystemConfigurationRepository,
            IGenericRepository<ParkingGate> parkingGateRepository, ParkingBusinessClock clock, ISender sender, IServiceScopeFactory scopes)
        {
            _clock = clock;
            _sender = sender;
            _scopes = scopes;
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _parkingMembershipsRepository = parkingMembershipsRepository ?? throw new ArgumentNullException(nameof(parkingMembershipsRepository));
            _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _byteVerificationService = byteVerificationService ?? throw new ArgumentNullException(nameof(byteVerificationService));
            _tokenReader = tokenReader ?? throw new ArgumentNullException(nameof(tokenReader));
            _pricingSchemaRepository = pricingSchemaRepository ?? throw new ArgumentNullException(nameof(pricingSchemaRepository));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _realtimeNotifier = realtimeNotifier ?? throw new ArgumentNullException(nameof(realtimeNotifier));
            _parkingSystemConfigurationRepository = parkingSystemConfigurationRepository ?? throw new ArgumentNullException(nameof(parkingSystemConfigurationRepository));
            _parkingGateRepository = parkingGateRepository ?? throw new ArgumentNullException(nameof(parkingGateRepository));
        }

        public async Task<Result<AddTicketCommandResponse>> Handle(
            AddTicketCommand request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var configSpec = new GetParkingSystemConfigurationForUpdateSpecification();

                var configuration = await _parkingSystemConfigurationRepository
                    .FirstOrDefaultWithSpecAsync(configSpec, cancellationToken);

                if (configuration is null)
                {
                    _logger.LogWarning("Parking system configuration not found while adding ticket.");

                    return Result<AddTicketCommandResponse>.Fail(
                        new Error(
                            Code: "Configuration.NotFound",
                            Message: ErrorMessage.Configuration_NotFound,
                            Type: ErrorType.NotFound));
                }

                var tokenInfo = _httpContextAccessor.HttpContext?.ReadToken(_tokenReader);

                if (tokenInfo is null || !tokenInfo.GateId.HasValue || !tokenInfo.UserId.HasValue)
                {
                    _logger.LogWarning("Token info missing GateId/UserId while adding ticket.");

                    return Result<AddTicketCommandResponse>.Fail(
                        new Error(
                            Code: "Token.GateOrUser.NotFound",
                            Message: ErrorMessage.TokenInfo_Missing,
                            Type: ErrorType.NotFound));
                }

                var gateInfo = await _parkingGateRepository
                    .GetByIdAsync(tokenInfo.GateId.Value, cancellationToken);

                if (gateInfo is null)
                {
                    _logger.LogWarning(
                        "Gate info not found while adding ticket. GateId={GateId}",
                        tokenInfo.GateId.Value);

                    return Result<AddTicketCommandResponse>.Fail(
                        new Error(
                            Code: "Gate.NotFound",
                            Message: ErrorMessage.GateNotFound,
                            Type: ErrorType.NotFound));
                }

                if (gateInfo.GateType != GateType.Entrance)
                    return Result<AddTicketCommandResponse>.Fail(new Error("Gate.InvalidForEntry",
                        ErrorMessage.GateNotFound, ErrorType.Conflict));

                if (gateInfo.HasLpr)
                {
                    if (request.VehicleNumber == null)
                    {
                        return Result<AddTicketCommandResponse>.Fail(
                            new Error(
                                Code: "VehicleNumber.RequiredForLPR",
                                Message: ErrorMessage.VehicleNumberRequired,
                                Type: ErrorType.Validation));
                    }
                }
                if (gateInfo.HasScanner is not null && gateInfo.HasScanner.Value == true)
                {
                    if (request.ScannerPhoto == null)
                    {
                        return Result<AddTicketCommandResponse>.Fail(
                                new Error(
                                    Code: "ScannerPhoto.RequiredForScannerGate",
                                    Message: ErrorMessage.ScannerPhotoRequired,
                                    Type: ErrorType.Validation));
                    }
                }
                if (request.IsSubscriber)
                {
                    return await HandleSubscriberTicketAsync(
                        request,
                        configuration,
                        tokenInfo.GateId.Value,
                        tokenInfo.UserId.Value,
                        gateInfo,
                        cancellationToken);
                }

                return await HandleNormalTicketAsync(
                    request,
                    configuration,
                    tokenInfo.GateId.Value,
                    tokenInfo.UserId.Value,
                    gateInfo,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AddTicket operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding ticket.");

                return Result<AddTicketCommandResponse>.Fail(
                    new Error(
                        Code: "Ticket.Add.Unexpected",
                        Message: ErrorMessage.TicketAdd_Unexpected,
                        Type: ErrorType.Infrastructure));
            }
        }

        private async Task<Result<AddTicketCommandResponse>> HandleSubscriberTicketAsync(
            AddTicketCommand request,
            ParkingSystemConfiguration configuration,
            Guid gateId,
            Guid userId,
            ParkingGate gateInfo,
            CancellationToken cancellationToken)
        {
            var cardSpec = new GetCardSummaryByIdSpec(
                request.CardNumber ?? string.Empty,
                request.VehicleNumber ?? string.Empty);

            var subscriber = await _parkingMembershipsRepository
                .FirstOrDefaultWithSpecAsync(cardSpec, cancellationToken);

            if (subscriber is null)
            {
                _logger.LogWarning(
                    "Subscriber card not found. CardNumber={CardNumber}, VehicleNumber={VehicleNumber}",
                    request.CardNumber,
                    request.VehicleNumber);

                return Result<AddTicketCommandResponse>.Fail(
                    new Error(
                        Code: "Subscriber.Card.NotFound",
                        Message: "المشترك غير موجود ",
                        Type: ErrorType.NotFound));
            }

            var issuedAtUtc = _clock.UtcNow;
            var now = _clock.ToBusinessTime(issuedAtUtc);

            if (now < subscriber.CreatedAt || now >= subscriber.EndDate)
            {
                _logger.LogWarning(
                    "Invalid subscriber card date. CardNumber={CardNumber}, VehicleNumber={VehicleNumber}, Start={Start}, End={End}, Now={Now}",
                    subscriber.CardNumber,
                    subscriber.VehicleNumber,
                    subscriber.CreatedAt,
                    subscriber.EndDate,
                    now);

                return Result<AddTicketCommandResponse>.Fail(
                    new Error(
                        Code: "Subscriber.Card.InvalidDate",
                        Message: ErrorMessage.Subscriber_Card_InvalidDate,
                        Type: ErrorType.Validation));
            }

            var activeSubscriberTicketSpec = new GetActiveSubscriberTicketSpec(
                cardNumber: subscriber.CardNumber,
                subscriberNationalId: subscriber.NationalId,
                vehicleNumber: subscriber.VehicleNumber);

            var activeSubscriberTicket = await _ticketRepository
                .FirstOrDefaultWithSpecAsync(activeSubscriberTicketSpec, cancellationToken);

            if (activeSubscriberTicket is not null)
            {
                _logger.LogWarning(
                    "Subscriber is already inside park. SubscriberNationalId={SubscriberNationalId}, CardNumber={CardNumber}, ActiveTicketId={TicketId}",
                    subscriber.NationalId,
                    subscriber.CardNumber,
                    activeSubscriberTicket.Id);

                return Result<AddTicketCommandResponse>.Fail(
                    new Error(
                        Code: "Subscriber.AlreadyInsidePark",
                        Message: ErrorMessage.Subscriber_AlreadyInsidePark,
                        Type: ErrorType.Validation));
            }

            var ticketEntity = Ticket.Create(
                startDate: now,
                price: 0m,
                gateId: gateId,
                userId: userId, businessDate: DateOnly.FromDateTime(now), pricingMode: configuration.PriceType);

            ticketEntity.SetSubscriber(
                nationalId: subscriber.NationalId,
                vehicleNumber: subscriber.VehicleNumber,
                cardNumber: subscriber.CardNumber);

            var persisted = await PersistWithAttachmentsAsync(ticketEntity, request, cancellationToken);
            if (persisted.IsFailure)
                return Result<AddTicketCommandResponse>.Fail(persisted.Errors);

            await NotifyDashboardTicketAddedAsync(
                ticketEntity,
                gateInfo,
                isSubscriber: true,
                cancellationToken);

            var response = BuildResponse(
                ticketEntity,
                qrCode: Array.Empty<byte>(),
                ticketInfo: ticketEntity.UniqueCode);

            return Result<AddTicketCommandResponse>.Ok(response);
        }

        private async Task<Result<AddTicketCommandResponse>> HandleNormalTicketAsync(
            AddTicketCommand request,
            ParkingSystemConfiguration configuration,
            Guid gateId,
            Guid userId,
            ParkingGate gateInfo,
            CancellationToken cancellationToken)
        {
            var issuedAtUtc = _clock.UtcNow;
            var now = _clock.ToBusinessTime(issuedAtUtc);

            Ticket ticketEntity;

            if (configuration.PriceType == PriceType.Enter)
            {
                if (request.PricingSchemaId is null)
                {
                    _logger.LogWarning("Configuration has PriceType=Enter but no PricingSchemaId.");

                    return Result<AddTicketCommandResponse>.Fail(
                        new Error(
                            Code: "Configuration.PricingSchema.Missing",
                            Message: ErrorMessage.PricingSchemaNotConfiguredForEnterPriceType,
                            Type: ErrorType.Validation));
                }

                var pricingScheme = await _pricingSchemaRepository
                    .GetByIdAsync(request.PricingSchemaId.Value, cancellationToken);

                if (pricingScheme is null)
                {
                    _logger.LogWarning(
                        "Pricing scheme not found. PricingSchemaId={PricingSchemaId}",
                        request.PricingSchemaId.Value);

                    return Result<AddTicketCommandResponse>.Fail(
                        new Error(
                            Code: "PricingSchema.NotFound",
                            Message: ErrorMessage.PricingSchema_NotFound,
                            Type: ErrorType.NotFound));
                }
                if (!pricingScheme.IsEnterPriceSchema)
                {
                    _logger.LogWarning(
                        "Invalid pricing scheme for enter ticket. PricingSchemaId={PricingSchemaId}",
                        request.PricingSchemaId.Value);

                    return Result<AddTicketCommandResponse>.Fail(
                        new Error(
                            Code: "EnterPriceSchema.InvalidForEnterTicket",
                            Message: ErrorMessage.EnterPriceSchema_InvalidForEnterTicket,
                            Type: ErrorType.Validation));
                }
                ticketEntity = Ticket.Create(
                    startDate: now,
                    price: pricingScheme.Salary,
                    gateId: gateId,
                    userId: userId, businessDate: DateOnly.FromDateTime(now), pricingMode: configuration.PriceType);

                ticketEntity.ConfirmBasePayment(userId, gateId, issuedAtUtc);

            }
            else
            {
                ticketEntity = Ticket.Create(
                    startDate: now,
                    price: 0m,
                    gateId: gateId,
                    userId: userId, businessDate: DateOnly.FromDateTime(now), pricingMode: configuration.PriceType);
            }

            if (!string.IsNullOrWhiteSpace(request.VehicleNumber))
            {
                ticketEntity.SetVehicleNumber(request.VehicleNumber);
            }

            var generatedQr = GenerateTicketQrCode(ticketEntity);
            var response = BuildResponse(ticketEntity, generatedQr.QrCode, generatedQr.QrText);
            var persisted = await PersistWithAttachmentsAsync(ticketEntity, request, cancellationToken);
            if (persisted.IsFailure)
                return Result<AddTicketCommandResponse>.Fail(persisted.Errors);

            await NotifyDashboardTicketAddedAsync(
                ticketEntity, gateInfo, isSubscriber: false, cancellationToken);
            return Result<AddTicketCommandResponse>.Ok(response);
        }

        private async Task<Result> PersistWithAttachmentsAsync(Ticket ticket, AddTicketCommand request, CancellationToken ct)
        {
            var newFiles = new List<string>();
            var persistenceAttempted = false;
            try
            {
                var files = request.TicketsImages?.AsEnumerable() ?? [];
                if (request.ScannerPhoto is { } scannerPhoto)
                    files = files.Append(scannerPhoto);
                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();
                    var path = await _mediaService.SaveAsync(file, FileNames.TicketAttachments);
                    newFiles.Add(path);
                    ticket.AddAttachment(path);
                }

                persistenceAttempted = true;
                var persisted = await _sender.Send(new PersistIssuedTicket(ticket), ct);
                if (persisted.IsFailure)
                    await CleanupFilesAsync(ticket.Id, newFiles, persistenceAttempted);
                return persisted;
            }
            catch
            {
                await CleanupFilesAsync(ticket.Id, newFiles, persistenceAttempted);
                throw;
            }
        }

        private async Task CleanupFilesAsync(Guid ticketId, List<string> paths, bool persistenceAttempted)
        {
            if (paths.Count == 0)
                return;
            try
            {
                if (persistenceAttempted)
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    if (await db.AnyWithSpecAsync(new Matching<Ticket>(x => x.Id == ticketId), timeout.Token) ||
                        await db.AnyWithSpecAsync(new Matching<TicketArchive>(x => x.Id == ticketId), timeout.Token))
                        return;
                }
                foreach (var path in paths)
                    _mediaService.RemoveFromFolder(path, FileNames.TicketAttachments);
            }
            catch (Exception error)
            {
                _logger.LogWarning(error, "Could not verify issuance or remove temporary media for ticket {TicketId}", ticketId);
            }
        }

        private async Task NotifyDashboardTicketAddedAsync(
            Ticket ticketEntity,
            ParkingGate gateInfo,
            bool isSubscriber,
            CancellationToken cancellationToken)
        {
            try
            {
                var gateNumber = gateInfo.GateType == GateType.Entrance
                    ? $"{ErrorMessage.EntranceGate} {gateInfo.GateNumber}"
                    : $"{ErrorMessage.ExitGate} {gateInfo.GateNumber}";

                var payload = new TicketAddedNotification
                {
                    TicketId = ticketEntity.Id,
                    GateId = ticketEntity.GateId,
                    StartDate = ticketEntity.StartDate,
                    Price = ticketEntity.Price,
                    IsSubscriber = isSubscriber,
                    VehicleNumber = ticketEntity.VehicleNumber,
                    GateNumber = gateNumber
                };

                await _realtimeNotifier.NotifyTicketAddedAsync(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send realtime notification for ticket {TicketId}",
                    ticketEntity.Id);
            }
        }

        private GeneratedTicketQr GenerateTicketQrCode(Ticket ticketEntity)
        {
            var byte5 = _byteVerificationService
                .GenerateComplexByte5FromGuid(ticketEntity.UniqueGuidPart);

            var combinedBytes = ticketEntity.UniqueGuidPart
                .Concat(new[] { byte5 })
                .ToArray();

            if (combinedBytes.Length != 5)
            {
                throw new InvalidOperationException(
                    $"Generated QR bytes must be exactly 5 bytes, but got {combinedBytes.Length} bytes.");
            }

            var qrText = Convert.ToHexString(combinedBytes);

            _logger.LogInformation(
                "Ticket QR generated as hexadecimal text. TicketId={TicketId}, UniqueCode={UniqueCode}, QrText={QrText}",
                ticketEntity.Id,
                ticketEntity.UniqueCode,
                qrText);

            var qrCode = _qrCodeService.GenerateQRCodeFromText(qrText);

            return new GeneratedTicketQr(
                QrCode: qrCode,
                QrText: qrText);
        }

        private static AddTicketCommandResponse BuildResponse(
            Ticket ticketEntity,
            byte[] qrCode,
            string ticketInfo)
        {
            return new AddTicketCommandResponse
            {
                CreatedAt = ticketEntity.StartDate,
                ValidExitDate = ticketEntity.ValidExitDate,
                EntryConfirmationStatus = ticketEntity.EntryConfirmationStatus.ToString(),
                IsCashierCollected = ticketEntity.IsCashierCollected,
                Price = ticketEntity.Price,
                QrCode = qrCode ?? Array.Empty<byte>(),
                TicketId = ticketEntity.Id,

                // For normal tickets this is now the exact QR text:
                // Example: 75A714A3C3
                TicketInfo = ticketInfo
            };
        }

        private sealed record GeneratedTicketQr(
            byte[] QrCode,
            string QrText);
    }
}