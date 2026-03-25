using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Feature.TicketsManagement.Command.CalculateSalary;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.AddSantctionCommand
{
    public sealed record AddSanctionCommandHandler : ICommandHandler<AddSanctionCommand, CalculateSalaryCommandResponse>
    {
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IGenericRepository<Sanction> _repo;
        public AddSanctionCommandHandler(IGenericRepository<Ticket> ticketRepository, IGenericRepository<Sanction> repo)
        {
            _ticketRepository = ticketRepository;
            _repo = repo;
        }
        public async Task<Result<CalculateSalaryCommandResponse>> Handle(AddSanctionCommand request, CancellationToken cancellationToken)
        {
            var ticketEntity = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticketEntity is null)
            {
                return Result<CalculateSalaryCommandResponse>.Fail(
                    new Error(
                        Code: "Ticket.NotFound",
                        Message: ErrorMessage.Ticket_NotFound,
                        Type: ErrorType.NotFound));
            }

            throw new NotImplementedException();
        }
    }
}