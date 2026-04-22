using FluentValidation;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.MemberShipExistByVehicleNumber
{
    internal sealed class MemberShipExistByVehicleNumberQueryValidator
        : AbstractValidator<MemberShipExistByVehicleNumberQuery>
    {
        public MemberShipExistByVehicleNumberQueryValidator()
        {
            RuleFor(x => x.VehicleNumber).NotEmpty()
                .NotNull()
                .WithMessage("Vehicle number is required");
        }
    }
}