using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Domain.Entities;
using NPark.Domain.Resource;
using System.Text.RegularExpressions;

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
            var text = NormalizeInputText(request.Text);

            _logger.LogInformation(
                "ValidateExitByText started. TextLength={TextLength}, LooksLikeQrHex={LooksLikeQrHex}, LooksLikeBase64={LooksLikeBase64}",
                text.Length,
                LooksLikeExactFiveByteHex(text),
                LooksLikeBase64(text));

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

            // 1) Try QR first:
            // New expected text:
            // CA692015F4
            if (LooksLikeExactFiveByteHex(text) || LooksLikeBase64(text))
            {
                ticket = await GetTicketByQrAsync(text, cancellationToken);

                if (ticket is not null)
                {
                    matchType = LooksLikeExactFiveByteHex(text)
                        ? "QrCodeHex"
                        : "QrCode";
                }
            }

            // 2) Fallback to CardNumber
            if (ticket is null)
            {
                ticket = await GetTicketByCardNumberAsync(text, cancellationToken);

                if (ticket is not null)
                {
                    matchType = "CardNumber";
                }
            }

            // 3) Fallback to VehicleNumber
            if (ticket is null)
            {
                ticket = await GetTicketByVehicleNumberAsync(text, cancellationToken);

                if (ticket is not null)
                {
                    matchType = "VehicleNumber";
                }
            }

            if (ticket is null)
            {
                _logger.LogWarning(
                    "ValidateExitByText stopped. No ticket found. Text={Text}",
                    text);

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

            return Result<ValidateExitByTextCommandResponse>.Ok(
                new ValidateExitByTextCommandResponse
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
                });
        }

        private async Task<Ticket?> GetTicketByQrAsync(
            string qrCode,
            CancellationToken cancellationToken)
        {
            var bytes = NormalizeQrBytes(qrCode);

            if (bytes is null)
            {
                _logger.LogWarning(
                    "QR normalization failed. QR could not be converted to valid 5-byte format. Input={Input}",
                    qrCode);

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
            {
                return null;
            }

            var uniquePart = bytes[..4];

            var uniqueCode = Convert.ToHexString(uniquePart);

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
            var cleanQr = NormalizeInputText(qrCode);

            // New path:
            // CA692015F4 => CA 69 20 15 F4
            if (LooksLikeExactFiveByteHex(cleanQr))
            {
                try
                {
                    var hexBytes = Convert.FromHexString(cleanQr);

                    _logger.LogInformation(
                        "QR decoded using 10-character hexadecimal path. BytesLength={BytesLength}, BytesHex={BytesHex}",
                        hexBytes.Length,
                        ToSpacedHex(hexBytes));

                    return hexBytes;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "QR hex decode failed. QrText={QrText}",
                        cleanQr);

                    return null;
                }
            }

            // Old path:
            // Base64 must decode to exactly 5 bytes.
            try
            {
                var exactBytes = _byteVerificationService.DecodeBase64ToBytes(cleanQr);

                _logger.LogInformation(
                    "QR decoded using exact 5-byte Base64 decoder. BytesLength={BytesLength}, BytesHex={BytesHex}",
                    exactBytes.Length,
                    ToSpacedHex(exactBytes));

                return exactBytes;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(
                    ex,
                    "Exact 5-byte Base64 QR decode failed. Trying legacy MQTT wrapped QR fallback.");
            }

            // Legacy wrapped path:
            // Base64 => 00 B7 + inner data + 85 BB E5 EB 00
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
                    "Legacy wrapped QR rejected. Prefix not matched. ExpectedPrefix={ExpectedPrefix}, DecodedBytesHex={DecodedBytesHex}",
                    ToSpacedHex(prefix),
                    ToSpacedHex(decodedBytes));

                return null;
            }

            if (!EndsWith(decodedBytes, suffix))
            {
                _logger.LogWarning(
                    "Legacy wrapped QR rejected. Suffix not matched. ExpectedSuffix={ExpectedSuffix}, DecodedBytesHex={DecodedBytesHex}",
                    ToSpacedHex(suffix),
                    ToSpacedHex(decodedBytes));

                return null;
            }

            var innerLength = decodedBytes.Length - prefix.Length - suffix.Length;

            if (innerLength <= 0)
            {
                _logger.LogWarning(
                    "Legacy wrapped QR rejected. Inner data is empty. DecodedBytesHex={DecodedBytesHex}",
                    ToSpacedHex(decodedBytes));

                return null;
            }

            var innerBytes = decodedBytes
                .Skip(prefix.Length)
                .Take(innerLength)
                .ToArray();

            if (innerBytes.Length == 5)
            {
                return innerBytes;
            }

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
                    catch
                    {
                        isValidCandidate = false;
                    }

                    _logger.LogInformation(
                        "Testing legacy QR 5-byte candidate. Offset={Offset}, CandidateHex={CandidateHex}, IsValidCandidate={IsValidCandidate}",
                        i,
                        ToSpacedHex(candidate),
                        isValidCandidate);

                    if (isValidCandidate)
                    {
                        return candidate;
                    }
                }
            }

            _logger.LogWarning(
                "Legacy wrapped QR rejected. No valid 5-byte candidate found. InnerBytesHex={InnerBytesHex}",
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

        private static string NormalizeInputText(string? input)
        {
            return (input ?? string.Empty)
                .Trim()
                .Trim('"')
                .Replace("{", string.Empty)
                .Replace("}", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace(":", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase)
                .ToUpperInvariant();
        }

        private static bool LooksLikeExactFiveByteHex(string input)
        {
            var clean = NormalizeInputText(input);

            return clean.Length == 10 &&
                   Regex.IsMatch(clean, "^[0-9A-F]{10}$");
        }

        private static bool LooksLikeBase64(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var clean = input.Trim().Trim('"');

            if (clean.Length % 4 != 0)
                return false;

            return Regex.IsMatch(
                clean,
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