using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.LprSignalRManagement.Command.VehicleDetected
{
    public sealed class LprVehicleDetectedCommandValidator
         : AbstractValidator<LprVehicleDetectedCommand>
    {
        public LprVehicleDetectedCommandValidator()
        {
            RuleFor(x => x.LprIp)
                .NotEmpty()
                .WithMessage(ErrorMessage.LprIpRequired)
                .MaximumLength(100)
                .WithMessage(ErrorMessage.LprIpMaxLength);

            RuleFor(x => x.VehicleNumber)
                .NotEmpty()
                .WithMessage(ErrorMessage.VehicleNumberRequired)
                .MaximumLength(50)
                .WithMessage(ErrorMessage.VehicleNumberMaxLength);
        }
    }
}