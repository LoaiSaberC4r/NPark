using FluentValidation;
using Microsoft.AspNetCore.Http;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Command.Update
{
    public sealed class UpdateParkingMembershipCommandValidator
        : AbstractValidator<UpdateParkingMembershipCommand>
    {
        public UpdateParkingMembershipCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Membership Id is required.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ErrorMessage.Name_Required);

            RuleFor(x => x.Phone)
                .NotEmpty()
                .WithMessage(ErrorMessage.Phone_Required)
                .Matches(@"^01[0-9]{9}$")
                .WithMessage(ErrorMessage.Invalid_PhoneNumber);

            RuleFor(x => x.NationalId)
                .NotEmpty()
                .WithMessage(ErrorMessage.Invalid_NationalId)
                .Matches(@"^\d{14}$")
                .WithMessage(ErrorMessage.Invalid_NationalId);

            RuleFor(x => x.VehicleNumber)
                .NotEmpty()
                .WithMessage(ErrorMessage.VehicleNumber_Required);

            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .WithMessage(ErrorMessage.CardNumber_Require);

            RuleFor(x => x.PricingSchemeId)
                .NotEmpty()
                .WithMessage(ErrorMessage.PricingSchemeId);

            RuleForEach(x => x.NewVehicleImages)
                .Must(BeAValidImage!)
                .WithMessage(ErrorMessage.Invalid_Image)
                .When(x => x.NewVehicleImages is { Count: > 0 });
        }

        private static bool BeAValidImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            return allowedExtensions.Contains(ext);
        }
    }
}