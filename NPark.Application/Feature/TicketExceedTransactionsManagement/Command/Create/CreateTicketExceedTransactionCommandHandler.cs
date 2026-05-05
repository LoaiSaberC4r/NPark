using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.QrCode;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.Create
{
    internal sealed class CreateTicketExceedTransactionCommandHandler
         : ICommandHandler<CreateTicketExceedTransactionCommand, CreateTicketExceedTransactionCommandResponse>
    {
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<TicketExceedTransaction> _ticketExceedTransactionRepository;
        private readonly IGenericRepository<Sanction> _sanctionRepository;
        private readonly IGenericRepository<ParkingGate> _parkingGateRepository;
        private readonly IGenericRepository<User> _userRepository;

        private readonly IQRCodeService _qrCodeService;
        private readonly IByteVerificationService _byteVerificationService;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenReader _tokenReader;
        private readonly ILogger<CreateTicketExceedTransactionCommandHandler> _logger;

        public CreateTicketExceedTransactionCommandHandler(
            IGenericRepository<Ticket> ticketRepository,
            IGenericRepository<TicketExceedTransaction> ticketExceedTransactionRepository,
            IGenericRepository<Sanction> sanctionRepository,
            IGenericRepository<ParkingGate> parkingGateRepository,
            IGenericRepository<User> userRepository,
            IQRCodeService qrCodeService,
            IByteVerificationService byteVerificationService,
            IHttpContextAccessor httpContextAccessor,
            ITokenReader tokenReader,
            ILogger<CreateTicketExceedTransactionCommandHandler> logger)
        {
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _ticketExceedTransactionRepository = ticketExceedTransactionRepository ?? throw new ArgumentNullException(nameof(ticketExceedTransactionRepository));
            _sanctionRepository = sanctionRepository ?? throw new ArgumentNullException(nameof(sanctionRepository));
            _parkingGateRepository = parkingGateRepository ?? throw new ArgumentNullException(nameof(parkingGateRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

            _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
            _byteVerificationService = byteVerificationService ?? throw new ArgumentNullException(nameof(byteVerificationService));

            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _tokenReader = tokenReader ?? throw new ArgumentNullException(nameof(tokenReader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<CreateTicketExceedTransactionCommandResponse>> Handle(
            CreateTicketExceedTransactionCommand request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // ---------------------------------------------------------------
                // 1) Read token info: GateId + UserId
                // ---------------------------------------------------------------
                var tokenInfo = _httpContextAccessor.HttpContext?.ReadToken(_tokenReader);

                if (tokenInfo is null || !tokenInfo.GateId.HasValue || !tokenInfo.UserId.HasValue)
                {
                    _logger.LogWarning("Token info missing GateId/UserId while creating ticket exceed transaction.");

                    return Result<CreateTicketExceedTransactionCommandResponse>.Fail(
                        new Error(
                            Code: "Token.GateOrUser.NotFound",
                            Message: ErrorMessage.TokenInfo_Missing,
                            Type: ErrorType.NotFound));
                }

                var gateId = tokenInfo.GateId.Value;
                var userCollectId = tokenInfo.UserId.Value;

                // ---------------------------------------------------------------
                // 2) Validate Gate exists
                // ---------------------------------------------------------------
                var gate = await _parkingGateRepository.GetByIdAsync(gateId, cancellationToken);

                if (gate is null)
                {
                    _logger.LogWarning(
                        "Gate not found while creating ticket exceed transaction. GateId: {GateId}",
                        gateId);

                    return Result<CreateTicketExceedTransactionCommandResponse>.Fail(
                        new Error(
                            Code: "Gate.NotFound",
                            Message: ErrorMessage.GateNotFound,
                            Type: ErrorType.NotFound));
                }

                // ---------------------------------------------------------------
                // 3) Validate User exists
                // ---------------------------------------------------------------
                var userCollector = await _userRepository.GetByIdAsync(userCollectId, cancellationToken);

                if (userCollector is null)
                {
                    _logger.LogWarning(
                        "User collector not found while creating ticket exceed transaction. UserId: {UserId}",
                        userCollectId);

                    return Result<CreateTicketExceedTransactionCommandResponse>.Fail(
                        new Error(
                            Code: "User.NotFound",
                            Message: ErrorMessage.User_NotFound,
                            Type: ErrorType.NotFound));
                }

                // ---------------------------------------------------------------
                // 4) Get Ticket
                // ---------------------------------------------------------------
                var ticketEntity = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);

                if (ticketEntity is null)
                {
                    _logger.LogWarning(
                        "Ticket not found while creating ticket exceed transaction. TicketId: {TicketId}",
                        request.TicketId);

                    return Result<CreateTicketExceedTransactionCommandResponse>.Fail(
                        new Error(
                            Code: "Ticket.NotFound",
                            Message: ErrorMessage.Ticket_NotFound,
                            Type: ErrorType.NotFound));
                }

                // ---------------------------------------------------------------
                // 5) Prevent duplicate transaction
                // Important: Transaction.Id = TicketId
                // ---------------------------------------------------------------
                var existingTransaction = await _ticketExceedTransactionRepository
                    .GetByIdAsync(request.TicketId, cancellationToken);

                if (existingTransaction is not null)
                {
                    _logger.LogWarning(
                        "Ticket exceed transaction already exists. TicketId: {TicketId}",
                        request.TicketId);

                    return Result<CreateTicketExceedTransactionCommandResponse>.Fail(
                        new Error(
                            Code: "TicketExceedTransaction.AlreadyExists",
                            Message: ErrorMessage.TicketExceedTransaction_AlreadyExists,
                            Type: ErrorType.Conflict));
                }

                // ---------------------------------------------------------------
                // 6) Generate SAME ticket QR as AddTicket
                // ---------------------------------------------------------------
                var generatedQr = GenerateTicketQrCode(ticketEntity);

                // ---------------------------------------------------------------
                // 7) Get Sanctions + calculate total exceed amount
                // ---------------------------------------------------------------
                var distinctSanctionIds = request.SanctionIds
                    .Distinct()
                    .ToList();

                var sanctions = new List<Sanction>();

                foreach (var sanctionId in distinctSanctionIds)
                {
                    var sanction = await _sanctionRepository.GetByIdAsync(sanctionId, cancellationToken);

                    if (sanction is null)
                    {
                        _logger.LogWarning(
                            "Sanction not found while creating ticket exceed transaction. SanctionId: {SanctionId}",
                            sanctionId);

                        return Result<CreateTicketExceedTransactionCommandResponse>.Fail(
                            new Error(
                                Code: "Sanction.NotFound",
                                Message: ErrorMessage.Sanction_NotFound,
                                Type: ErrorType.NotFound));
                    }

                    sanctions.Add(sanction);
                }

                var totalExceedAmount = sanctions.Sum(x => x.Amount);
                var oldExceedPrice = ticketEntity.ExceedPrice;

                // ---------------------------------------------------------------
                // 8) Update ticket exceed price
                // ---------------------------------------------------------------
                ticketEntity.SetExceedPrice(totalExceedAmount);

                // ---------------------------------------------------------------
                // 9) Create transaction snapshot
                // SuperVisorCollectorId = null initially
                // ---------------------------------------------------------------
                var transaction = TicketExceedTransaction.Create(
                    ticket: ticketEntity,
                    oldExceedPrice: oldExceedPrice,
                    exceedAmount: totalExceedAmount,
                    gateId: gateId,
                    userCollectId: userCollectId,
                    sanctions: sanctions);

                await _ticketExceedTransactionRepository.AddAsync(transaction, cancellationToken);

                // Saves:
                // 1) Ticket.ExceedPrice update
                // 2) TicketExceedTransaction insert
                // 3) Many-to-many sanctions join rows
                await _ticketExceedTransactionRepository.SaveChangesAsync(cancellationToken);
                await _ticketRepository.SaveChangesAsync(cancellationToken);

                // ---------------------------------------------------------------
                // 10) Build response
                // ---------------------------------------------------------------
                var response = new CreateTicketExceedTransactionCommandResponse
                {
                    TicketExceedTransactionId = transaction.Id,
                    TicketId = ticketEntity.Id,

                    ExceedAmount = transaction.ExceedAmount,
                    TicketPrice = transaction.TicketPrice,
                    TotalPriceAfterExceed = transaction.TotalPriceAfterExceed,

                    GateId = transaction.GateId,
                    UserCollectId = transaction.UserCollectId,
                    SuperVisorCollectorId = transaction.SuperVisorCollectorId,

                    SanctionIds = distinctSanctionIds,
                    CreatedAt = transaction.CreatedAt,

                    QrCode = generatedQr.QrCode,
                    TicketInfo = generatedQr.QrText
                };

                return Result<CreateTicketExceedTransactionCommandResponse>.Ok(response);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("CreateTicketExceedTransaction operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating ticket exceed transaction.");

                return Result<CreateTicketExceedTransactionCommandResponse>.Fail(
                    new Error(
                        Code: "TicketExceedTransaction.Create.Unexpected",
                        Message: ErrorMessage.TicketExceedTransaction_Create_Unexpected,
                        Type: ErrorType.Infrastructure));
            }
        }

        private (byte[] QrCode, string QrText) GenerateTicketQrCode(Ticket ticketEntity)
        {
            if (ticketEntity.UniqueGuidPart is null || ticketEntity.UniqueGuidPart.Length != 4)
            {
                throw new InvalidOperationException(
                    $"Ticket UniqueGuidPart must be exactly 4 bytes. TicketId={ticketEntity.Id}");
            }

            var byte5 = _byteVerificationService
                .GenerateComplexByte5FromGuid(ticketEntity.UniqueGuidPart);

            var combinedBytes = ticketEntity.UniqueGuidPart
                .Concat(new[] { byte5 })
                .ToArray();

            if (combinedBytes.Length != 5)
            {
                throw new InvalidOperationException(
                    $"Generated QR bytes must be exactly 5 bytes, but got {combinedBytes.Length} bytes. TicketId={ticketEntity.Id}");
            }

            var qrText = Convert.ToHexString(combinedBytes);

            _logger.LogInformation(
                "Ticket QR generated for exceed transaction. TicketId={TicketId}, UniqueCode={UniqueCode}, QrText={QrText}",
                ticketEntity.Id,
                ticketEntity.UniqueCode,
                qrText);

            var qrCode = _qrCodeService.GenerateQRCodeFromText(qrText);

            return (qrCode, qrText);
        }
    }
}