using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.ValidateExitByVehicleNumber
{
    internal sealed class ValidateExitByVehicleNumberQueryHandler
           : IQueryHandler<ValidateExitByVehicleNumberQuery, ValidateExitByVehicleNumberQueryResponse>
    {
        private readonly IGenericRepository<Ticket> _ticketReadRepository;

        public ValidateExitByVehicleNumberQueryHandler(IGenericRepository<Ticket> repository)
        {
            _ticketReadRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<ValidateExitByVehicleNumberQueryResponse>> Handle(ValidateExitByVehicleNumberQuery request, CancellationToken cancellationToken)
        {
            var normalizedVehicleNumber = request.VehicleNumber.Trim();
            var now = DateTime.Now;
            var dayStart = now.Date;
            var dayEnd = dayStart.AddDays(1);

            var spec = new GetTodayTicketByVehicleNumberSpec(
         normalizedVehicleNumber,
         dayStart,
         dayEnd);
            var ticket = await _ticketReadRepository.FirstOrDefaultWithSpecAsync(spec, cancellationToken);
            if (ticket is null)
            {
                return Result<ValidateExitByVehicleNumberQueryResponse>.Ok
                        (new ValidateExitByVehicleNumberQueryResponse

                        { IsValidForExit = false });
            }

            return Result<ValidateExitByVehicleNumberQueryResponse>.Ok
                    (new ValidateExitByVehicleNumberQueryResponse
                    { IsValidForExit = true });
        }
    }
}