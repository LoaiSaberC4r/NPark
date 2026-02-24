using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.SupportManagement.Query.Activities
{
    public sealed record ActivitiesSupportQuery : IQuery<ActivitiesSupportQueryResponse>
    {
        public Guid Id { get; init; }
    }
}