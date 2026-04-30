using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText
{
    internal sealed class ValidateExitByTextCommandHandler
        : ICommandHandler<ValidateExitByTextCommand, ValidateExitByTextCommandResponse>
    {
        private readonly IByteVerificationService _byteVerificationService;
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly ILogger<ValidateExitByTextCommandHandler> _logger;

        public ValidateExitByTextCommandHandler(
            IByteVerificationService byteVerificationService,
            IGenericRepository<Ticket> ticketRepository,
            ILogger<ValidateExitByTextCommandHandler> logger)
        {
            _byteVerificationService = byteVerificationService
                ?? throw new ArgumentNullException(nameof(byteVerificationService));

            _ticketRepository = ticketRepository
                ?? throw new ArgumentNullException(nameof(ticketRepository));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ValidateExitByTextCommandResponse>> Handle(
            ValidateExitByTextCommand request,
            CancellationToken cancellationToken)
        {
            var text = request.Text?.Trim();

            _logger.LogInformation(
                "ValidateExitByText started. TextLength={TextLength}, LooksLikeBase64={LooksLikeBase64}",
                text?.Length ?? 0,
                !string.IsNullOrWhiteSpace(text) && LooksLikeBase64(text));

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("ValidateExitByText stopped. Text is required.");

                return Result<ValidateExitByTextCommandResponse>.Fail(
                    new Error(
                        Code: "ExitValidation.TextRequired",
                        Message: ErrorMessage.QrCode_Require,
                        Type: ErrorType.Validation));
            }

            Ticket? ticket = null;
            string? matchType = null;

            // 1) Try QR Code first
            if (LooksLikeBase64(text))
            {
                _logger.LogInformation(
                    "ValidateExitByText trying QR lookup. TextLength={TextLength}",
                    text.Length);

                ticket = await GetTicketByQrAsync(text, cancellationToken);

                _logger.LogInformation(
                    "ValidateExitByText QR lookup completed. Found={Found}",
                    ticket is not null);

                if (ticket is not null)
                {
                    matchType = "QrCode";
                }
            }
            else
            {
                _logger.LogInformation(
                    "ValidateExitByText skipped QR lookup because text is not Base64-like. TextLength={TextLength}",
                    text.Length);
            }

            // 2) Fallback to CardNumber
            if (ticket is null)
            {
                _logger.LogInformation(
                    "ValidateExitByText trying CardNumber lookup.");

                ticket = await GetTicketByCardNumberAsync(text, cancellationToken);

                _logger.LogInformation(
                    "ValidateExitByText CardNumber lookup completed. Found={Found}",
                    ticket is not null);

                if (ticket is not null)
                {
                    matchType = "CardNumber";
                }
            }

            // 3) Fallback to VehicleNumber
            if (ticket is null)
            {
                _logger.LogInformation(
                    "ValidateExitByText trying VehicleNumber lookup.");

                ticket = await GetTicketByVehicleNumberAsync(text, cancellationToken);

                _logger.LogInformation(
                    "ValidateExitByText VehicleNumber lookup completed. Found={Found}",
                    ticket is not null);

                if (ticket is not null)
                {
                    matchType = "VehicleNumber";
                }
            }

            // 4) No ticket found
            if (ticket is null)
            {
                _logger.LogWarning(
                    "ValidateExitByText stopped. No ticket found.");

                return Result<ValidateExitByTextCommandResponse>.Ok(
                    new ValidateExitByTextCommandResponse
                    {
                        InputText = text,
                        MatchType = null,
                        TicketId = null,
                        TicketNumber = null,
                        CardNumber = null,
                        VehicleNumber = null,
                        StartDate = null,
                        EndDate = null,
                        IsValidForExit = false
                    });
            }

            // 5) Validate exit rule
            var now = DateTime.Now;

            var isValidForExit =
                ticket.StartDate.Date == now.Date &&
                ticket.EndDate is null;

            _logger.LogInformation(
                "ValidateExitByText completed. TicketId={TicketId}, MatchType={MatchType}, TicketNumber={TicketNumber}, StartDate={StartDate}, EndDate={EndDate}, Now={Now}, IsValidForExit={IsValidForExit}",
                ticket.Id,
                matchType,
                ticket.UniqueCode,
                ticket.StartDate,
                ticket.EndDate,
                now,
                isValidForExit);

            var response = new ValidateExitByTextCommandResponse
            {
                InputText = text,
                MatchType = matchType,
                TicketId = ticket.Id,
                TicketNumber = ticket.UniqueCode,
                CardNumber = ticket.CardNumber,
                VehicleNumber = ticket.VehicleNumber,
                StartDate = ticket.StartDate,
                EndDate = ticket.EndDate,
                IsValidForExit = isValidForExit
            };

            return Result<ValidateExitByTextCommandResponse>.Ok(response);
        }

        private async Task<Ticket?> GetTicketByQrAsync(
            string qrCode,
            CancellationToken cancellationToken)
        {
            var bytes = NormalizeQrBytes(qrCode);

            if (bytes is null)
            {
                _logger.LogWarning(
                    "QR normalization failed. QR could not be converted to valid 5-byte format.");

                return null;
            }

            _logger.LogInformation(
                "QR normalized successfully. NormalizedBytesHex={NormalizedBytesHex}, BytesLength={BytesLength}",
                ToSpacedHex(bytes),
                bytes.Length);

            if (bytes.Length != 5)
            {
                _logger.LogWarning(
                    "QR rejected. Normalized bytes must be exactly 5 bytes. BytesLength={BytesLength}, BytesHex={BytesHex}",
                    bytes.Length,
                    ToSpacedHex(bytes));

                return null;
            }

            var isByte5Valid = _byteVerificationService.VerifyByte5(bytes);

            _logger.LogInformation(
                "QR byte verification completed. IsByte5Valid={IsByte5Valid}, BytesHex={BytesHex}",
                isByte5Valid,
                ToSpacedHex(bytes));

            if (!isByte5Valid)
                return null;

            var uniquePart = bytes[..4];

            var uniqueCode = BitConverter
                .ToString(uniquePart)
                .Replace("-", "");

            _logger.LogInformation(
                "QR unique code extracted. UniqueCode={UniqueCode}, UniquePartHex={UniquePartHex}",
                uniqueCode,
                ToSpacedHex(uniquePart));

            var spec = new TicketByUniqueCodeSpecification(uniqueCode);

            var ticket = await _ticketRepository.FirstOrDefaultWithSpecAsync(
                spec,
                cancellationToken);

            _logger.LogInformation(
                "Ticket lookup by QR unique code completed. UniqueCode={UniqueCode}, Found={Found}, TicketId={TicketId}",
                uniqueCode,
                ticket is not null,
                ticket?.Id);

            return ticket;
        }

        private byte[]? NormalizeQrBytes(string qrCode)
        {
            var cleanQr = qrCode
                .Trim()
                .Trim('"');

            // ----------------------------------------------------
            // 1) Old path:
            // Base64 must decode to exactly 5 bytes.
            // ----------------------------------------------------
            try
            {
                var exactBytes = _byteVerificationService.DecodeBase64ToBytes(cleanQr);

                _logger.LogInformation(
                    "QR decoded using exact 5-byte decoder. BytesLength={BytesLength}, BytesHex={BytesHex}",
                    exactBytes.Length,
                    ToSpacedHex(exactBytes));

                return exactBytes;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(
                    ex,
                    "Exact 5-byte QR decode failed. Trying MQTT wrapped QR fallback.");
            }

            // ----------------------------------------------------
            // 2) MQTT wrapped path:
            // Base64 => 00 B7 + inner data + 85 BB E5 EB 00
            // ----------------------------------------------------
            byte[] decodedBytes;

            try
            {
                decodedBytes = Convert.FromBase64String(cleanQr);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "QR Base64 decode failed completely.");

                return null;
            }

            _logger.LogInformation(
                "QR decoded using raw Base64. DecodedBytesLength={DecodedBytesLength}, DecodedBytesHex={DecodedBytesHex}",
                decodedBytes.Length,
                ToSpacedHex(decodedBytes));

            var prefix = new byte[] { 0x00, 0xB7 };
            var suffix = new byte[] { 0x85, 0xBB, 0xE5, 0xEB, 0x00 };

            if (!StartsWith(decodedBytes, prefix))
            {
                _logger.LogWarning(
                    "MQTT wrapped QR rejected. Prefix not matched. ExpectedPrefix={ExpectedPrefix}, DecodedBytesHex={DecodedBytesHex}",
                    ToSpacedHex(prefix),
                    ToSpacedHex(decodedBytes));

                return null;
            }

            if (!EndsWith(decodedBytes, suffix))
            {
                _logger.LogWarning(
                    "MQTT wrapped QR rejected. Suffix not matched. ExpectedSuffix={ExpectedSuffix}, DecodedBytesHex={DecodedBytesHex}",
                    ToSpacedHex(suffix),
                    ToSpacedHex(decodedBytes));

                return null;
            }

            var innerLength = decodedBytes.Length - prefix.Length - suffix.Length;

            if (innerLength <= 0)
            {
                _logger.LogWarning(
                    "MQTT wrapped QR rejected. Inner data is empty. DecodedBytesHex={DecodedBytesHex}",
                    ToSpacedHex(decodedBytes));

                return null;
            }

            var innerBytes = decodedBytes
                .Skip(prefix.Length)
                .Take(innerLength)
                .ToArray();

            _logger.LogInformation(
                "MQTT wrapped QR inner bytes extracted. InnerLength={InnerLength}, InnerBytesHex={InnerBytesHex}",
                innerBytes.Length,
                ToSpacedHex(innerBytes));

            // Case:
            // inner data itself is exactly the old 5-byte QR.
            if (innerBytes.Length == 5)
            {
                _logger.LogInformation(
                    "MQTT wrapped QR inner bytes are exactly 5 bytes. InnerBytesHex={InnerBytesHex}",
                    ToSpacedHex(innerBytes));

                return innerBytes;
            }

            // Case:
            // inner data is larger than 5 bytes.
            // Try every 5-byte window and select the candidate that passes VerifyByte5.
            //
            // Example:
            // Inner: A5 36 76 DA 5B 9F
            // Candidate 0: A5 36 76 DA 5B
            // Candidate 1: 36 76 DA 5B 9F
            if (innerBytes.Length > 5)
            {
                for (var i = 0; i <= innerBytes.Length - 5; i++)
                {
                    var candidate = innerBytes
                        .Skip(i)
                        .Take(5)
                        .ToArray();

                    var isValidCandidate = false;

                    try
                    {
                        isValidCandidate = _byteVerificationService.VerifyByte5(candidate);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(
                            ex,
                            "Testing MQTT QR 5-byte candidate threw exception. Offset={Offset}, CandidateHex={CandidateHex}",
                            i,
                            ToSpacedHex(candidate));

                        isValidCandidate = false;
                    }

                    _logger.LogInformation(
                        "Testing MQTT QR 5-byte candidate. Offset={Offset}, CandidateHex={CandidateHex}, IsValidCandidate={IsValidCandidate}",
                        i,
                        ToSpacedHex(candidate),
                        isValidCandidate);

                    if (isValidCandidate)
                    {
                        _logger.LogInformation(
                            "MQTT QR candidate selected. Offset={Offset}, CandidateHex={CandidateHex}",
                            i,
                            ToSpacedHex(candidate));

                        return candidate;
                    }
                }
            }

            _logger.LogWarning(
                "MQTT wrapped QR rejected. No valid 5-byte candidate found. InnerBytesHex={InnerBytesHex}",
                ToSpacedHex(innerBytes));

            return null;
        }

        private async Task<Ticket?> GetTicketByCardNumberAsync(
            string cardNumber,
            CancellationToken cancellationToken)
        {
            var spec = new TicketByCardNumberSpecification(cardNumber);

            var ticket = await _ticketRepository.FirstOrDefaultWithSpecAsync(
                spec,
                cancellationToken);

            _logger.LogInformation(
                "Ticket lookup by CardNumber completed. Found={Found}, TicketId={TicketId}",
                ticket is not null,
                ticket?.Id);

            return ticket;
        }

        private async Task<Ticket?> GetTicketByVehicleNumberAsync(
            string vehicleNumber,
            CancellationToken cancellationToken)
        {
            var spec = new TicketByVehicleNumberSpecification(vehicleNumber);

            var ticket = await _ticketRepository.FirstOrDefaultWithSpecAsync(
                spec,
                cancellationToken);

            _logger.LogInformation(
                "Ticket lookup by VehicleNumber completed. Found={Found}, TicketId={TicketId}",
                ticket is not null,
                ticket?.Id);

            return ticket;
        }

        private static bool LooksLikeBase64(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (input.Length % 4 != 0)
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(
                input,
                @"^[A-Za-z0-9\+/=]+$");
        }

        private static bool StartsWith(byte[] source, byte[] prefix)
        {
            if (source.Length < prefix.Length)
                return false;

            for (var i = 0; i < prefix.Length; i++)
            {
                if (source[i] != prefix[i])
                    return false;
            }

            return true;
        }

        private static bool EndsWith(byte[] source, byte[] suffix)
        {
            if (source.Length < suffix.Length)
                return false;

            var offset = source.Length - suffix.Length;

            for (var i = 0; i < suffix.Length; i++)
            {
                if (source[offset + i] != suffix[i])
                    return false;
            }

            return true;
        }

        private static string ToSpacedHex(byte[] bytes)
        {
            if (bytes.Length == 0)
                return string.Empty;

            return string.Join(" ", bytes.Select(b => b.ToString("X2")));
        }
    }
}