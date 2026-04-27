using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
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

        public ValidateExitByTextCommandHandler(
            IByteVerificationService byteVerificationService,
            IGenericRepository<Ticket> ticketRepository)
        {
            _byteVerificationService = byteVerificationService
                ?? throw new ArgumentNullException(nameof(byteVerificationService));

            _ticketRepository = ticketRepository
                ?? throw new ArgumentNullException(nameof(ticketRepository));
        }

        public async Task<Result<ValidateExitByTextCommandResponse>> Handle(
            ValidateExitByTextCommand request,
            CancellationToken cancellationToken)
        {
            var text = request.Text?.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
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
                ticket = await GetTicketByQrAsync(text, cancellationToken);

                if (ticket is not null)
                {
                    matchType = "QrCode";
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

            // 4) No ticket found
            if (ticket is null)
            {
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
            byte[] bytes;

            try
            {
                bytes = _byteVerificationService.DecodeBase64ToBytes(qrCode);
            }
            catch
            {
                return null;
            }

            if (bytes is null || bytes.Length < 5)
                return null;

            if (!_byteVerificationService.VerifyByte5(bytes))
                return null;

            var uniquePart = bytes[..4];
            var uniqueCode = BitConverter
                .ToString(uniquePart)
                .Replace("-", "");

            var spec = new TicketByUniqueCodeSpecification(uniqueCode);

            return await _ticketRepository.FirstOrDefaultWithSpecAsync(
                spec,
                cancellationToken);
        }

        private async Task<Ticket?> GetTicketByCardNumberAsync(
            string cardNumber,
            CancellationToken cancellationToken)
        {
            var spec = new TicketByCardNumberSpecification(cardNumber);

            return await _ticketRepository.FirstOrDefaultWithSpecAsync(
                spec,
                cancellationToken);
        }

        private async Task<Ticket?> GetTicketByVehicleNumberAsync(
            string vehicleNumber,
            CancellationToken cancellationToken)
        {
            var spec = new TicketByVehicleNumberSpecification(vehicleNumber);

            return await _ticketRepository.FirstOrDefaultWithSpecAsync(
                spec,
                cancellationToken);
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
    }
}