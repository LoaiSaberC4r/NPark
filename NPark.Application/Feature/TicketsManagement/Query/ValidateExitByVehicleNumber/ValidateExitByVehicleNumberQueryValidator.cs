using FluentValidation;

namespace NPark.Application.Feature.TicketsManagement.Query.ValidateExitByVehicleNumber
{
    internal sealed class ValidateExitByVehicleNumberQueryValidator
       : AbstractValidator<ValidateExitByVehicleNumberQuery>
    {
        public ValidateExitByVehicleNumberQueryValidator()
        {
            RuleFor(x => x.VehicleNumber)
                .NotEmpty()
                .MaximumLength(50);
        }
    }
}