using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Delete
{
    public sealed class DeletePricingSchemaCommandHandler : ICommandHandler<DeletePricingSchemaCommand>
    {
        private readonly IGenericRepository<PricingScheme> _repository;

        public DeletePricingSchemaCommandHandler(IGenericRepository<PricingScheme> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result> Handle(DeletePricingSchemaCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
            _repository.Delete(entity);
            await _repository.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
    }
}