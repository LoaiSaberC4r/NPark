using BuildingBlock.Domain.Specification;
using CRM.Application.Feature.SupportManagement.Query.Activities;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.SupportSpecification
{
    internal sealed class ActivationSupporterSpec : Specification<Supporter, ActivitiesSupportQueryResponse>
    {
        public ActivationSupporterSpec(Guid id)
        {
            AddCriteria(x => x.Id == id);
            Select(x => new ActivitiesSupportQueryResponse
            {
                AssignedCount = x.AssignedTickets.Count,
                ClosedCount = x.ClosedTickets.Count,
                RejectedCount = x.RejectedTickets.Count
            });
        }
    }
}