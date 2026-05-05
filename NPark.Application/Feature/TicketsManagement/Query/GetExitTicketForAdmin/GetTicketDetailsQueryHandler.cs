using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.SharedDto;
using NPark.Application.Feature.TicketsManagement.Query.GetTicketDetails;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.GetExitTicketForAdmin
{
    public sealed class GetTicketDetailsQueryHandler
         : IQueryHandler<GetTicketDetailsQuery, Pagination<GetTicketDetailsQueryResponse>>
    {
        private readonly IGenericRepository<Ticket> _ticketRepository;

        public GetTicketDetailsQueryHandler(IGenericRepository<Ticket> ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public Task<Result<Pagination<GetTicketDetailsQueryResponse>>> Handle(
            GetTicketDetailsQuery request,
            CancellationToken cancellationToken)
        {
            var spec = new GetTicketDetailsSpecification(request);

            var result = _ticketRepository.GetWithSpec(spec);

            var response = new Pagination<GetTicketDetailsQueryResponse>(
                request.PageNumber,
                request.PageSize,
                result.count,
                result.data.ToList());

            return Task.FromResult(Result<Pagination<GetTicketDetailsQueryResponse>>.Ok(response));
        }
    }
}