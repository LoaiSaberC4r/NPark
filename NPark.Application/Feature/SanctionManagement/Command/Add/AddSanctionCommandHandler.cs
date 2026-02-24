using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.SanctionManagement.Command.Add
{
    public sealed class AddSanctionCommandHandler : ICommandHandler<AddSanctionCommand>
    {
        private readonly IGenericRepository<Sanction> _repo;

        public AddSanctionCommandHandler(IGenericRepository<Sanction> repository)
        {
            _repo = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result> Handle(AddSanctionCommand request, CancellationToken cancellationToken)
        {
            var entity = Sanction.Create(request.Name, request.Description is null ? string.Empty : request.Description, request.Amount);

            await _repo.AddAsync(entity, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
    }
}