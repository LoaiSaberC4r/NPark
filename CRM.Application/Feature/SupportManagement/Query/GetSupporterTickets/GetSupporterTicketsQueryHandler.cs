using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Domain.Entities;

namespace CRM.Application.Feature.SupportManagement.Query.GetSupporterTickets
{
    internal sealed record GetSupporterTicketsQueryHandler(IGenericRepository<Supporter> supporterRepository)
        : IQueryHandler<GetSupporterTicketsQuery, GetSupporterTicketsQueryResponse>
    {
        public async Task<Result<GetSupporterTicketsQueryResponse>> Handle(GetSupporterTicketsQuery request, CancellationToken cancellationToken)
        {
            //var spec = new GetSupporterTicketsSpec();
            throw new NotImplementedException();
        }
    }
}