using FluentValidation;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Delete
{
    internal sealed class DeletePricingSchemaCommandValidator : AbstractValidator<DeletePricingSchemaCommand>
    {
        public DeletePricingSchemaCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage(ErrorMessage.PricingSchemeId);
        }
    }
}