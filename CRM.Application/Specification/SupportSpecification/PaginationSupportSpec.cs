using BuildingBlock.Domain.Specification;
using CRM.Application.Feature.SupportManagement.Query.Paginate;
using CRM.Domain.Entities;

namespace CRM.Application.Specification.SupportSpecification
{
    internal sealed class PaginationSupportSpec : Specification<Supporter, PaginateSupporterQueryResponse>
    {
        public PaginationSupportSpec()
        {
            Select(s => new PaginateSupporterQueryResponse
            {
                Id = s.Id,
                Email = s.Email,
                AssignedTicketCount = s.AssignedTickets.Count,
                ClosedTicketCount = s.ClosedTickets.Count,
                Phone = s.PhoneNumber
            });
        }
    }
}