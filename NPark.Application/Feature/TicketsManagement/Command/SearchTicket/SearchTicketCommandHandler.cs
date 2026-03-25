using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Feature.TicketsManagement.Query.GetTicketForEntryGate;
using NPark.Application.Specifications.TicketSpecification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Command.SearchTicket
{
    public sealed class SearchTicketCommandHandler : ICommandHandler<SearchTicketCommand, IReadOnlyList<GetTicketInfo>>
    {
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<TicketArchive> _ticketArchiveRepository;

        public SearchTicketCommandHandler(IGenericRepository<Ticket> ticketRepository, IGenericRepository<TicketArchive> ticketArchiveRepository)
        {
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _ticketArchiveRepository = ticketArchiveRepository ?? throw new ArgumentNullException(nameof(ticketArchiveRepository));
        }

        public async Task<Result<IReadOnlyList<GetTicketInfo>>> Handle(SearchTicketCommand request, CancellationToken cancellationToken)
        {
            var spec = new SearchTicketSpecification(request);
            var result = await _ticketRepository.ListWithSpecAsync(spec, cancellationToken);
            var ticketArchiveSpec = new SearchTicketArchiveSpecification(request);
            var resultArchive = await _ticketArchiveRepository.ListWithSpecAsync(ticketArchiveSpec, cancellationToken);
            result.AddRange(resultArchive);
            return Result<IReadOnlyList<GetTicketInfo>>.Ok(result);
        }
    }
}