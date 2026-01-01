using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Application.Abstraction;
using CRM.Application.Abstraction.Security;
using CRM.Application.Shared.Dto;
using CRM.Domain.Entities;
using CRM.Domain.Resources;
using Microsoft.AspNetCore.Http;

namespace CRM.Application.Feature.SupportManagement.Command.CloseTicket
{
    internal sealed record CloseTicketTicketCommandHandler : ICommandHandler<CloseTicketTicketCommand>
    {
        private readonly ITokenReader _tokenReader;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IEmailSender _emailSender;

        public CloseTicketTicketCommandHandler(IGenericRepository<Ticket> ticketRepository,
            ITokenReader tokenReader,
            IHttpContextAccessor httpContextAccessor, IEmailSender emailSender)
        {
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _tokenReader = tokenReader ?? throw new ArgumentNullException(nameof(tokenReader));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        public async Task<Result> Handle(CloseTicketTicketCommand request, CancellationToken cancellationToken)
        {
            var token = _httpContextAccessor?.HttpContext?.ReadToken(_tokenReader);
            if (token is null || token.UserId is null)
            {
                return Result.Fail(new Error(
                    Code: "user.NotFound",
                    Message: ErrorMessage.WrongLoginCredintial,
                    Type: ErrorType.Security
                    ));
            }
            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return Result.Fail(new Error(
                    Code: "ticket.NotFound",
                    Message: "Ticket not found",
                    Type: ErrorType.NotFound
                    ));
            }
            ticket.CloseTicket(token.UserId.Value);
            await _ticketRepository.SaveChangesAsync(cancellationToken);
            _emailSender.SendAsync(
          new EmailMessage(
              To: ticket.Email,
              Subject: "Ticket Closed",
              HtmlBody: $"<p>Ticket closed  With Subject {ticket.Subject} has been closed and the description : {request.Description ?? string.Empty}" +
              $" has been sent to you.</p><p><b>Id:</b> {ticket.Subject} .</p><p><b>Description:</b  : {request.Description ?? string.Empty}</p>"),
          cancellationToken);
            throw new NotImplementedException();
        }
    }
}