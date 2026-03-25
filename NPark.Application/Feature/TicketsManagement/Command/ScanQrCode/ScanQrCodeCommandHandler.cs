using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Abstraction.Security;
using NPark.Application.Feature.TicketsManagement.Query.GetExitTicketForAdmin;
using NPark.Application.Specifications.TicketSpecification;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.ScanQrCode
{
    internal sealed class ScanQrCodeCommandHandler
        (IByteVerificationService _byteVerificationService,
    IGenericRepository<Ticket> _ticketRepository)
        : ICommandHandler<ScanQrCodeCommand, GetExitTicketForAdminQueryResponse>
    {
        public async Task<Result<GetExitTicketForAdminQueryResponse>> Handle(ScanQrCodeCommand request, CancellationToken cancellationToken)
        {
            // ---------------------------
            // 1) Decode & validate QR code
            // ---------------------------
            byte[] bytes;
            try
            {
                bytes = _byteVerificationService.DecodeBase64ToBytes(request.QrCode);
            }
            catch (FormatException ex)
            {
                return Result<GetExitTicketForAdminQueryResponse>.Fail(
                    new Error(
                        Code: "QrCode.InvalidFormat",
                        Message: ErrorMessage.QrInvalid,
                        Type: ErrorType.Validation));
            }

            if (bytes is null || bytes.Length < 5)
            {
                return Result<GetExitTicketForAdminQueryResponse>.Fail(
                    new Error(
                        Code: "QrCode.InvalidBytes",
                        Message: ErrorMessage.QrInvalid,
                        Type: ErrorType.Validation));
            }

            if (!_byteVerificationService.VerifyByte5(bytes))
            {
                return Result<GetExitTicketForAdminQueryResponse>.Fail(
                    new Error(
                        Code: "QrCode.InvalidChecksum",
                        Message: ErrorMessage.QrInvalid,
                        Type: ErrorType.Validation));
            }
            // ---------------------------
            // 2) Retrieve Ticket
            // Unique part = first 4 bytes of QR
            // ---------------------------
            var ticketEntity = await GetTicketEntity(bytes, cancellationToken);
            if (ticketEntity is null)
            {
                return Result<GetExitTicketForAdminQueryResponse>.Fail(
                    new Error(
                        Code: "Ticket.NotFound",
                        Message: ErrorMessage.Ticket_NotFound,
                        Type: ErrorType.NotFound));
            }
            var response = new GetExitTicketForAdminQueryResponse
            {
                Id = ticketEntity.Id,
                Price = ticketEntity.Price,
                StartDate = ticketEntity.StartDate,
                isCollectedByCashier = ticketEntity.IsCashierCollected,
                TicketNumber = BitConverter.ToString(ticketEntity.UniqueGuidPart).Replace("-", ""),
                TicketInfo = ticketEntity.VehicleNumber
            };
            return Result<GetExitTicketForAdminQueryResponse>.Ok(response);
        }

        private async Task<Ticket?> GetTicketEntity(byte[] qrBytes, CancellationToken cancellationToken)
        {
            // أول 4 Bytes هي الـ UniqueGuidPart المستخدمة في الـ Ticket
            var uniquePart = qrBytes[..4];
            var spec = new TicketByUniquePartSpecification(uniquePart);
            return await _ticketRepository.FirstOrDefaultWithSpecAsync(spec, cancellationToken);
        }
    }
}