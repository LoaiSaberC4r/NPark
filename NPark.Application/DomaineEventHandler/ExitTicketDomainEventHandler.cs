using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using NPark.Domain.DomainEvent;
using NPark.Domain.Entities;
using System.Data;

namespace NPark.Application.DomaineEventHandler
{
    public sealed class ExitTicketDomainEventHandler : IDomainEventHandler<ExitTicketDomainEvent>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ExitTicketDomainEventHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(ExitTicketDomainEvent notification, CancellationToken cancellationToken)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            try
            {
                var ticketArchive = TicketArchive.CreateFromTicket(notification.Ticket);

                await _unitOfWork
                    .Repository<TicketArchive>()
                    .AddAsync(ticketArchive, cancellationToken);

                _unitOfWork
                    .Repository<Ticket>()
                    .Delete(notification.Ticket);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}