using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Update
{
    public class UpdatePricingSchemaCommandValidator : AbstractValidator<UpdatePricingSchemaCommand>
    {
        private readonly IGenericRepository<PricingScheme> _repository;

        public UpdatePricingSchemaCommandValidator(IGenericRepository<PricingScheme> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage(ErrorMessage.PricingSchemeId)
                .MustAsync(async (id, cancellationToken) =>
                    await _repository.IsExistAsync(x => x.Id == id, cancellationToken))
                .WithMessage(ErrorMessage.NotFound);

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ErrorMessage.IsRequired);

            /*
             * Enter Price Schema Validation
             * هنا ممنوع نطلب DurationType / StartTime / EndTime
             * لأنهم بيتظبطوا جوه الـ Entity Method.
             */
            When(x => x.IsEnterPriceSchema, () =>
            {
                RuleFor(x => x.Price)
                    .GreaterThan(0)
                    .WithMessage(ErrorMessage.InvalidPrice);
            });

            /*
             * Normal Pricing Schema Validation
             */
            When(x => !x.IsEnterPriceSchema, () =>
            {
                RuleFor(x => x.DurationType)
                    .IsInEnum()
                    .WithMessage(ErrorMessage.IsRequired);

                RuleFor(x => x.StartTime)
                    .NotEmpty()
                    .WithMessage(ErrorMessage.IsRequired)
                    .When(x => !x.IsRepeated);

                RuleFor(x => x.EndTime)
                    .NotEmpty()
                    .WithMessage(ErrorMessage.IsRequired)
                    .When(x => !x.IsRepeated);

                RuleFor(x => x.Price)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(ErrorMessage.InvalidPrice);

                RuleFor(x => x.EndTime)
                    .GreaterThan(x => x.StartTime)
                    .WithMessage(ErrorMessage.Invalid_EndTime)
                    .When(x => !x.IsRepeated && x.StartTime.HasValue && x.EndTime.HasValue);

                RuleFor(x => x.IsRepeated)
                    .Must((command, isRepeated) =>
                        !(isRepeated && command.DurationType == DurationType.Days))
                    .WithMessage(ErrorMessage.Conflict_IsRepeated);

                RuleFor(x => x.TotalDays)
                    .NotEmpty()
                    .When(x => x.DurationType == DurationType.Days)
                    .WithMessage(ErrorMessage.TotalDay_Require)
                    .GreaterThan(0)
                    .When(x => x.DurationType == DurationType.Days)
                    .WithMessage(ErrorMessage.Invalid_TotalDays);

                RuleFor(x => x.TotalHours)
                    .NotEmpty()
                    .When(x => x.DurationType == DurationType.Hours)
                    .WithMessage(ErrorMessage.TotalHour_Require)
                    .GreaterThan(0)
                    .When(x => x.DurationType == DurationType.Hours)
                    .WithMessage(ErrorMessage.Invalid_TotalHour);
            });
        }
    }
}