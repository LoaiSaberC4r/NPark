using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.SearchTicket
{
    public sealed class SearchTicketQueryHandler
        : IQueryHandler<SearchTicketQuery, IReadOnlyList<SearchTicketQueryResponse>>
    {
        private readonly IGenericRepository<Ticket> _ticketRepository;

        public SearchTicketQueryHandler(IGenericRepository<Ticket> ticketRepository)
        {
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        }

        public async Task<Result<IReadOnlyList<SearchTicketQueryResponse>>> Handle(
            SearchTicketQuery request,
            CancellationToken cancellationToken)
        {
            var spec = new SearchTicketQuerySpecification(request);

            var tickets = await _ticketRepository.ListWithSpecAsync(spec, cancellationToken);

            return Result<IReadOnlyList<SearchTicketQueryResponse>>.Ok(tickets);
        }
    }
}