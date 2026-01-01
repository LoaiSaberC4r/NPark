using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Application.Specification.AccountSpecification;
using CRM.Domain.Entities;
using CRM.Domain.FileName;

namespace CRM.Application.Feature.AccountManagement.Query.GetSupporters
{
    internal sealed record GetSupportersQueryHandler : IQueryHandler<GetSupportersQuery, IReadOnlyList<GetSupportersQueryResponse>>
    {
        private readonly IGenericRepository<SuperAdmin> _superAdminRepository;
        public GetSupportersQueryHandler(IGenericRepository<SuperAdmin> superAdminRepository
            )
        {
            _superAdminRepository = superAdminRepository;
        }
        public async Task<Result<IReadOnlyList<GetSupportersQueryResponse>>> Handle(GetSupportersQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetSupportersSpec(RoleName.SupportRoleId);
            var supporters = await _superAdminRepository.ListWithSpecAsync(spec, cancellationToken);
            return Result<IReadOnlyList<GetSupportersQueryResponse>>.Ok(supporters);
        }
    }
}