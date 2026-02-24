using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Application.Specification.SupportSpecification;
using CRM.Domain.Entities;

namespace CRM.Application.Feature.SupportManagement.Query.Activities
{
    internal sealed record ActivitiesSupportQueryHandler(IGenericRepository<Supporter> supporterRepo)
        : IQueryHandler<ActivitiesSupportQuery, ActivitiesSupportQueryResponse>
    {
        public async Task<Result<ActivitiesSupportQueryResponse>> Handle(ActivitiesSupportQuery request, CancellationToken cancellationToken)
        {
            var spec = new ActivationSupporterSpec(request.Id);
            var response = await supporterRepo.FirstOrDefaultWithSpecAsync(spec, cancellationToken);
            return Result<ActivitiesSupportQueryResponse>.Ok(response ?? new ActivitiesSupportQueryResponse());
        }
    }
}