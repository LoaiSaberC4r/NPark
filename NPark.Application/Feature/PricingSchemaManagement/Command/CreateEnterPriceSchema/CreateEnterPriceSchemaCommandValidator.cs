using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.CreateEnterPriceSchema
{
    public sealed class CreateEnterPriceSchemaCommandValidator
          : AbstractValidator<CreateEnterPriceSchemaCommand>
    {
        public CreateEnterPriceSchemaCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ErrorMessage.EnterPriceSchema_Name_Required)
                .MaximumLength(100)
                .WithMessage(ErrorMessage.EnterPriceSchema_Name_MaxLength);

            RuleFor(x => x.Salary)
                .GreaterThan(0)
                .WithMessage(ErrorMessage.EnterPriceSchema_Salary_GreaterThanZero);
        }
    }
}