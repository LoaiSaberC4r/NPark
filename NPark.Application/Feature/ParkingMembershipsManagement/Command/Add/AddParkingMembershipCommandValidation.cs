using BuildingBlock.Application.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Command.Add
{
    public sealed class AddParkingMembershipCommandValidation : AbstractValidator<AddParkingMembershipCommand>
    {
        private IGenericRepository<PricingScheme> _pricingRepo;
        private IGenericRepository<ParkingMemberships> _parkingRepo;

        public AddParkingMembershipCommandValidation(IGenericRepository<PricingScheme> pricingRepo, IGenericRepository<ParkingMemberships> parkingRepo)
        {
            _pricingRepo = pricingRepo ?? throw new ArgumentNullException(nameof(pricingRepo));
            _parkingRepo = parkingRepo ?? throw new ArgumentNullException(nameof(parkingRepo));
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ErrorMessage.Name_Required);

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage(ErrorMessage.Phone_Required)
                .Matches(@"^01[0-9]{9}$").WithMessage(ErrorMessage.Invalid_PhoneNumber)
                .MustAsync(async (phone, token) => !await _parkingRepo.IsExistAsync(x => x.Phone == phone, token)).WithMessage(ErrorMessage.Unique_Phone);

            RuleFor(x => x.NationalId)
                .NotEmpty().WithMessage(ErrorMessage.IsRequired)
                .Matches(@"^\d{14}$").WithMessage(ErrorMessage.Invalid_NationalId)
                .MustAsync(async (id, token) => !await _parkingRepo.IsExistAsync(x => x.NationalId == id, token)).WithMessage(ErrorMessage.Unique_NationalId);

            RuleFor(x => x.VehicleNumber)
                .NotEmpty().WithMessage(ErrorMessage.VehicleNumber_Required);

            RuleFor(x => x.CardNumber)
                .NotEmpty().WithMessage(ErrorMessage.CardNumber_Required);

            RuleFor(x => x.PricingSchemeId)
                .NotEmpty().WithMessage(ErrorMessage.PricingSchemeId)
                .MustAsync(async (id, token) => await _pricingRepo.IsExistAsync(x => x.Id == id, token)).WithMessage(ErrorMessage.NotFound);

            RuleForEach(x => x.VehicleImage).
                Must(BeAValidImage!).WithMessage(ErrorMessage.Invalid_Image)
                .When(x => x.VehicleImage != null && x.VehicleImage is { Count: > 0 });
        }

        private bool BeAValidImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName)?.ToLower();
            return allowedExtensions.Contains(fileExtension);
        }
    }
}