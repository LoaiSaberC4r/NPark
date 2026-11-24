using NPark.Application.Services;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Specifications.TicketSpecification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.GetEntryTicketForAdmin
{
    public sealed class GetEntryTicketForAdminQueryHandler : IQueryHandler<GetEntryTicketForAdminQuery, IReadOnlyList<GetEntryTicketForAdminQueryResponse>>
    {
        private readonly ParkingBusinessClock _clock;
        private readonly IGenericRepository<Ticket> _repo;

        public GetEntryTicketForAdminQueryHandler(IGenericRepository<Ticket> repo, ParkingBusinessClock clock)
        {
            _clock = clock;
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Result<IReadOnlyList<GetEntryTicketForAdminQueryResponse>>> Handle(GetEntryTicketForAdminQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetAllTicketsEntryForTodaySpecification(_clock.LocalNow.Date);
            var respone = await _repo.ListWithSpecAsync(spec, cancellationToken);
            return Result<IReadOnlyList<GetEntryTicketForAdminQueryResponse>>.Ok(respone);
        }
    }
}