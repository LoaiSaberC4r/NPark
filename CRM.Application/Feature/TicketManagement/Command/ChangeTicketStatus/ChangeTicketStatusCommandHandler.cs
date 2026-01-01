using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Application.Abstraction;
using CRM.Application.Shared.Dto;
using CRM.Domain.Entities;

namespace CRM.Application.Feature.TicketManagement.Command.ChangeTicketStatus
{
    internal sealed class ChangeTicketStatusCommandHandler : ICommandHandler<ChangeTicketStatusCommand>
    {
        private readonly IGenericRepository<Ticket> _repository;
        private readonly IGenericRepository<SuperAdmin> _supportAdminRepository;
        private readonly IEmailSender _emailSender;

        public ChangeTicketStatusCommandHandler(IGenericRepository<Ticket> repository, IEmailSender emailSender, IGenericRepository<SuperAdmin> supportAdminRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _emailSender = emailSender;
            _supportAdminRepository = supportAdminRepository;
        }

        public async Task<Result> Handle(ChangeTicketStatusCommand request, CancellationToken cancellationToken)
        {
            //1-Get Ticket by Id
            var ticket = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (ticket is null)
                return Result.Fail(new Error(
                    Code: "TicketNotFound",
                    Message: " Ticket not found",
                    Type: ErrorType.NotFound
                    ));
            //2-Update get Sepporter by Id
            var supporter = await _supportAdminRepository.GetByIdAsync(request.SupportId, cancellationToken);
            if (supporter is null)
            {
                return Result.Fail(new Error(
                        Code: "SupporterNotFound",
                        Message: " Supporter not found",
                        Type: ErrorType.NotFound
                        ));
            }

            //3-Update Ticket
            ticket.AssignTicket(request.SupportId);
            ticket.UpdateSeverity(request.Severity);
            await _repository.SaveChangesAsync(cancellationToken);

            //4 - Send Email to request.Email
            _emailSender.SendAsync(
          new EmailMessage(
              To: supporter.Email,
              Subject: "Ticket Assigned",
              HtmlBody: $"<p>We New Ticket Assigned to you.</p><p><b>Id:</b> {ticket.Subject} .</p><p><b>Severity:</b> {ticket.Severity} .</p><p><b>Description:</b  : {request.Description ?? string.Empty}  .</p><p><b>Subject:</b> </p>"),
          cancellationToken);

            return Result.Ok();
        }
    }
}