using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Specifications.TicketSpecification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.ReportTickets
{
    internal sealed class ReportTicketsHandler(IGenericRepository<TicketArchive> repo) :
        IQueryHandler<ReportTicketsQuery, IReadOnlyCollection<ReportTicketsResponse>>
    {
        public async Task<Result<IReadOnlyCollection<ReportTicketsResponse>>> Handle(ReportTicketsQuery request, CancellationToken cancellationToken)
        {
            var spec = new TicketReportSpec(request);
            var result = await repo.ListWithSpecAsync(spec, cancellationToken);
            return Result<IReadOnlyCollection<ReportTicketsResponse>>.Ok(result);
        }
    }
}