using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.AddOrderSchema
{
    public sealed class AddOrderSchemaCommandValidation : AbstractValidator<AddOrderSchemaCommand>
    {
        private readonly IGenericRepository<PricingScheme> _pricingSchemaRepository;

        public AddOrderSchemaCommandValidation(IGenericRepository<PricingScheme> pricingSchemaRepository)
        {
            _pricingSchemaRepository = pricingSchemaRepository ?? throw new ArgumentNullException(nameof(pricingSchemaRepository));

            RuleFor(x => x.OrderSchema)
            .NotNull()
            .NotEmpty()
            .WithMessage(ErrorMessage.IsRequired);

            RuleForEach(x => x.OrderSchema).ChildRules(order =>
            {
                order.RuleFor(o => o.PricingSchemaId)
                    .NotEmpty()
                    .MustAsync(async (id, token) =>
                    await _pricingSchemaRepository.IsExistAsync(x => x.Id == id, token))
                    .WithMessage(ErrorMessage.PricingSchemeId)
                   .MustAsync(async (id, token) => await _pricingSchemaRepository.IsExistAsync(x => x.IsRepeated == true, token))
                   .WithMessage("Must be Repeated Entity");

                order.RuleFor(o => o.Count)
                    .GreaterThan(0)
                    .WithMessage(ErrorMessage.IsRequired);
            });
            RuleFor(x => x.OrderSchema)
    .Custom((list, ctx) =>
            {
                if (list is null || list.Count == 0)
                    return;

                var hasDuplicates = list
                    .GroupBy(x => x.Count)
                    .Any(g => g.Count() > 1);

                if (hasDuplicates)
                {
                    ctx.AddFailure(
                        "OrderSchema",
                        "القيم داخل Count لا يجب أن تتكرر / Count values in the list must be unique."
                    );
                }
            });
        }
    }
}