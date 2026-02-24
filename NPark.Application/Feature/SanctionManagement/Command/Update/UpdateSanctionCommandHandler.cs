using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.SanctionManagement.Command.Update
{
    public sealed class UpdateSanctionCommandHandler : ICommandHandler<UpdateSanctionCommand>
    {
        private readonly IGenericRepository<Sanction> _repo;

        public UpdateSanctionCommandHandler(IGenericRepository<Sanction> repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Result> Handle(UpdateSanctionCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result.Fail(new Error(
                    Code: "Sanction.NotFound",
                    Message: ErrorMessage.Sanction_NotFound,
                    Type: ErrorType.NotFound));
            }

            entity.Update(request.Name, request.Description, request.Amount);

            _repo.Update(entity);
            await _repo.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
    }
}