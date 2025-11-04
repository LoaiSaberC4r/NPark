using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Delete
{
    internal class DeletePricingSchemaCommandValidation : AbstractValidator<DeletePricingSchemaCommand>
    {
        private IGenericRepository<PricingScheme> _repository;

        public DeletePricingSchemaCommandValidation(IGenericRepository<PricingScheme> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            RuleFor(x => x.Id).NotEmpty().WithMessage(ErrorMessage.PricingSchemeId)
                .MustAsync(async (id, cancellationToken) =>
                await _repository.IsExistAsync(x => x.Id == id, cancellationToken))
                .WithMessage(ErrorMessage.NotFound);
        }
    }
}