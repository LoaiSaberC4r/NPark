using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.ScanQrCode
{
    internal sealed class ScanQrCodeCommandValidator : AbstractValidator<ScanQrCodeCommand>
    {
        public ScanQrCodeCommandValidator()
        {
            RuleFor(x => x.QrCode)
                .NotEmpty()
                .WithMessage(ErrorMessage.QrCode_Require)
                .Matches(@"^[A-Za-z0-9\+/=]+$") // Base64
                .WithMessage(ErrorMessage.QrInvalid);
        }
    }
}