using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.SanctionManagement.Query.GetAll
{
    internal sealed class GetAllSanctionsQueryHandler : IQueryHandler<GetAllSanctionsQuery, IReadOnlyList<SanctionDto>>
    {
        private readonly IGenericRepository<Sanction> _repo;

        public GetAllSanctionsQueryHandler(IGenericRepository<Sanction> repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Result<IReadOnlyList<SanctionDto>>> Handle(GetAllSanctionsQuery request, CancellationToken cancellationToken)
        {
            var entities = _repo.GetAll();

            var dtos = entities
                .Select(x => new SanctionDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Amount = x.Amount
                })
                .ToList()
                .AsReadOnly();

            return Result<IReadOnlyList<SanctionDto>>.Ok(dtos);
        }
    }
}