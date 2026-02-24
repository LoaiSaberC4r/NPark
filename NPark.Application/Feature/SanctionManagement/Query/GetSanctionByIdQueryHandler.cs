using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.SanctionManagement.Query
{
    internal sealed class GetSanctionByIdQueryHandler : IQueryHandler<GetSanctionByIdQuery, SanctionDto>
    {
        private readonly IGenericRepository<Sanction> _repo;

        public GetSanctionByIdQueryHandler(IGenericRepository<Sanction> repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Result<SanctionDto>> Handle(GetSanctionByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result<SanctionDto>.Fail(new Error(
                    Code: "Sanction.NotFound",
                    Message: ErrorMessage.Sanction_NotFound,
                    Type: ErrorType.NotFound));
            }

            return Result<SanctionDto>.Ok(new SanctionDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Amount = entity.Amount
            });
        }
    }
}