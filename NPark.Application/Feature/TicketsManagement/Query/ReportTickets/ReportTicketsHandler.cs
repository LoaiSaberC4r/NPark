using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Specifications.TicketSpecification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.ReportTickets
{
    internal sealed class ReportTicketsHandler(
        IGenericRepository<Ticket> ticketRepo,
        IGenericRepository<TicketArchive> ticketArchiveRepo)
        : IQueryHandler<ReportTicketsQuery, IReadOnlyCollection<ReportTicketsResponse>>
    {
        public async Task<Result<IReadOnlyCollection<ReportTicketsResponse>>> Handle(
            ReportTicketsQuery request,
            CancellationToken cancellationToken)
        {
            var activeTicketsSpec = new ActiveTicketReportSpec(request);
            var archivedTicketsSpec = new ArchivedTicketReportSpec(request);

            var activeTickets = await ticketRepo.ListWithSpecAsync(
                activeTicketsSpec,
                cancellationToken);

            var archivedTickets = await ticketArchiveRepo.ListWithSpecAsync(
                archivedTicketsSpec,
                cancellationToken);

            var result = activeTickets
                .Concat(archivedTickets)
                .OrderByDescending(x => x.EnterDate)
                .ThenByDescending(x => x.ExitDate)
                .ToList();

            return Result<IReadOnlyCollection<ReportTicketsResponse>>.Ok(result);
        }
    }
}