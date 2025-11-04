using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.ParkingSystemConfigurationManagement.Command.Update
{
    public class UpdateParkingConfigurationCommandValidator : AbstractValidator<UpdateParkingConfigurationCommand>
    {
        private readonly IGenericRepository<PricingScheme> _pricingSchemeRepository;

        public UpdateParkingConfigurationCommandValidator(IGenericRepository<PricingScheme> pricingSchemeRepository)
        {
            _pricingSchemeRepository = pricingSchemeRepository ?? throw new ArgumentNullException(nameof(pricingSchemeRepository));
            RuleFor(x => x.EntryGatesCount)
    .GreaterThanOrEqualTo(1)
    .WithMessage("Entry gates count must be at least 1. \nعدد بوابات الدخول يجب ألا يقل عن 1.");

            RuleFor(x => x.ExitGatesCount)
                .GreaterThanOrEqualTo(1)
                .WithMessage(ErrorMessage.Invalid_ExitGatesCount);

            RuleFor(x => x.PriceType)
                .IsInEnum()
                .WithMessage(ErrorMessage.Invalid_PriceType);

            RuleFor(x => x.VehiclePassengerData)
                .IsInEnum()
                .WithMessage(ErrorMessage.Invalid_VehiclePassengerData);

            RuleFor(x => x.PrintType)
                .IsInEnum()
                .WithMessage(ErrorMessage.Invalid_PrintType);

            // ===== Nullable / Optional Fields =====

            RuleFor(x => x.AllowedParkingSlots)
                .GreaterThan(0)
                .When(x => x.AllowedParkingSlots.HasValue)
                .WithMessage(ErrorMessage.Invalid_AllowedParkingSlots);

            // ===== Conditional Restriction =====
            // When PriceType = Enter → PricingSchemaId must be provided

            When(x => x.PriceType == PriceType.Enter, () =>
            {
                RuleFor(x => x.PricingSchemaId)
                    .NotNull()
                    .NotEqual(Guid.Empty)
                    .WithMessage(ErrorMessage.PricingSchemaId_RequiredWhenEnter)
                    .MustAsync(async (id, token) => await _pricingSchemeRepository.IsExistAsync(x => x.Id == id, token));
            });
        }
    }
}