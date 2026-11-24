using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Resource;
using System.Text.RegularExpressions;

namespace NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText
{
    internal sealed class ValidateExitByTextCommandHandler(IByteVerificationService verification, IUnitOfWork db,
        ITicketExitEligibilityService eligibility,
        ILogger<ValidateExitByTextCommandHandler> logger)
        : ICommandHandler<ValidateExitByTextCommand, ValidateExitByTextCommandResponse>
    {
        public async Task<Result<ValidateExitByTextCommandResponse>> Handle(ValidateExitByTextCommand request, CancellationToken ct)
        {
            var text = NormalizeInputText(request.Text);
            if (string.IsNullOrWhiteSpace(text))
                return Result<ValidateExitByTextCommandResponse>.Fail(new Error("ExitValidation.TextRequired",
                    ErrorMessage.QrCode_Require, ErrorType.Validation));

            var response = new ValidateExitByTextCommandResponse { InputText = text };
            Ticket? ticket = null;
            var match = "QrCode";
            if (LooksLikeExactFiveByteHex(text) || LooksLikeBase64(text))
            {
                var bytes = NormalizeQrBytes(text);
                if (bytes is { Length: 5 } && verification.VerifyByte5(bytes))
                {
                    var code = Convert.ToHexString(bytes.AsSpan(0, 4));
                    ticket = await db.FirstOrDefaultWithSpecAsync(new Matching<Ticket>(t => t.UniqueCode == code), ct);
                    if (ticket is null)
                    {
                        var archive = await db.FirstOrDefaultWithSpecAsync(new Matching<TicketArchive>(x => x.UniqueCode == code), ct);
                        if (archive != null)
                            return Result<ValidateExitByTextCommandResponse>.Ok(response with
                                { ReasonCode = "TicketArchived", TicketId = archive.Id, EndDate = archive.EndDate });
                    }
                    match = LooksLikeExactFiveByteHex(text) ? "QrCodeHex" : "QrCode";
                }
            }

            if (ticket is null && !request.QrOnly)
            {
                var candidates = await db.ListWithSpecAsync(new TicketByCardNumberSpecification(text), ct);
                match = "CardNumber";
                if (candidates.Count == 0)
                {
                    candidates = await db.ListWithSpecAsync(new TicketByVehicleNumberSpecification(text), ct);
                    match = "VehicleNumber";
                }
                if (candidates.Count > 1)
                    return Result<ValidateExitByTextCommandResponse>.Ok(response with { ReasonCode = "AmbiguousTicket" });
                ticket = candidates.SingleOrDefault();
            }
            if (ticket is null)
                return Result<ValidateExitByTextCommandResponse>.Ok(response);

            var assessment = await eligibility.EvaluateAsync(ticket, null, ct);
            return Result<ValidateExitByTextCommandResponse>.Ok(response with
            {
                MatchType = match,
                TicketId = ticket.Id,
                TicketNumber = ticket.UniqueCode,
                CardNumber = ticket.CardNumber,
                VehicleNumber = ticket.VehicleNumber,
                StartDate = ticket.StartDate,
                EndDate = ticket.EndDate,
                IsValidForExit = assessment.IsValidForExit,
                ReasonCode = assessment.ReasonCode,
                Eligibility = assessment
            });
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

                    logger.LogInformation(
                        "QR decoded using 10-character hexadecimal path. BytesLength={BytesLength}, BytesHex={BytesHex}",
                        hexBytes.Length,
                        ToSpacedHex(hexBytes));

                    return hexBytes;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
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
                var exactBytes = verification.DecodeBase64ToBytes(cleanQr);

                logger.LogInformation(
                    "QR decoded using exact 5-byte Base64 decoder. BytesLength={BytesLength}, BytesHex={BytesHex}",
                    exactBytes.Length,
                    ToSpacedHex(exactBytes));

                return exactBytes;
            }
            catch (Exception ex)
            {
                logger.LogInformation(
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
                logger.LogWarning(
                    ex,
                    "QR Base64 decode failed completely.");

                return null;
            }

            logger.LogInformation(
                "QR decoded using raw Base64. DecodedBytesLength={DecodedBytesLength}, DecodedBytesHex={DecodedBytesHex}",
                decodedBytes.Length,
                ToSpacedHex(decodedBytes));

            var prefix = new byte[] { 0x00, 0xB7 };
            var suffix = new byte[] { 0x85, 0xBB, 0xE5, 0xEB, 0x00 };

            if (!StartsWith(decodedBytes, prefix))
            {
                logger.LogWarning(
                    "Legacy wrapped QR rejected. Prefix not matched. ExpectedPrefix={ExpectedPrefix}, DecodedBytesHex={DecodedBytesHex}",
                    ToSpacedHex(prefix),
                    ToSpacedHex(decodedBytes));

                return null;
            }

            if (!EndsWith(decodedBytes, suffix))
            {
                logger.LogWarning(
                    "Legacy wrapped QR rejected. Suffix not matched. ExpectedSuffix={ExpectedSuffix}, DecodedBytesHex={DecodedBytesHex}",
                    ToSpacedHex(suffix),
                    ToSpacedHex(decodedBytes));

                return null;
            }

            var innerLength = decodedBytes.Length - prefix.Length - suffix.Length;

            if (innerLength <= 0)
            {
                logger.LogWarning(
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
                        isValidCandidate = verification.VerifyByte5(candidate);
                    }
                    catch
                    {
                        isValidCandidate = false;
                    }

                    logger.LogInformation(
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

            logger.LogWarning(
                "Legacy wrapped QR rejected. No valid 5-byte candidate found. InnerBytesHex={InnerBytesHex}",
                ToSpacedHex(innerBytes));

            return null;
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