using NPark.Application.Services;
using NPark.Domain.Enums;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Specifications.TicketSpecification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.GetTicketInsidePark
{
    public sealed class GetTicketInsideParkQueryHandler : IQueryHandler<GetTicketInsideParkQuery, IReadOnlyList<GetTicketInsideParkQueryResponse>>
    {
        private readonly IGenericRepository<Ticket> _ticketRepository;

        private readonly IUnitOfWork _db;

        public GetTicketInsideParkQueryHandler(IGenericRepository<Ticket> ticketRepository, IUnitOfWork db)
        {
            _db = db;
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        }

        public async Task<Result<IReadOnlyList<GetTicketInsideParkQueryResponse>>> Handle(GetTicketInsideParkQuery request, CancellationToken cancellationToken)
        {
            var awaitingArchive = await _db.ListWithSpecAsync(new Matching<GateOperation>(x =>
                x.Status == GateOperationStatus.Acknowledged && x.Type == GateOperationType.Exit &&
                x.TicketIdSnapshot != null), cancellationToken);
            var spec = new GetTicketInsideParkSpecification(awaitingArchive.Select(x => x.TicketIdSnapshot!.Value).ToArray());
            var response = await _ticketRepository.ListWithSpecAsync(spec, cancellationToken);
            return Result<IReadOnlyList<GetTicketInsideParkQueryResponse>>.Ok(response);
        }
    }
}