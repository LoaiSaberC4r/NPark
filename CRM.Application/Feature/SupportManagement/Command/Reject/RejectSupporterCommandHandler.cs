using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Application.Abstraction;
using CRM.Application.Abstraction.Security;
using CRM.Application.Shared.Dto;
using CRM.Domain.Entities;
using CRM.Domain.Resources;
using Microsoft.AspNetCore.Http;

namespace CRM.Application.Feature.SupportManagement.Command.Reject
{
    internal sealed record RejectSupporterCommandHandler(IGenericRepository<Ticket> ticketRepo,
            ITokenReader tokenReader,
            IHttpContextAccessor httpContextAccessor, IEmailSender emailSender

        ) : ICommandHandler<RejectSupporterCommand>
    {
        public async Task<Result> Handle(RejectSupporterCommand request, CancellationToken cancellationToken)
        {
            var token = httpContextAccessor?.HttpContext?.ReadToken(tokenReader);
            if (token is null || token.UserId is null)
            {
                return Result.Fail(new Error(
                    Code: "user.NotFound",
                    Message: ErrorMessage.WrongLoginCredintial,
                    Type: ErrorType.Security
                    ));
            }
            var ticket = await ticketRepo.GetByIdAsync(request.TicketId, cancellationToken);
            ticket!.RejectTicket(request.RejectReason);
            await ticketRepo.SaveChangesAsync(cancellationToken);
            emailSender.SendAsync(
          new EmailMessage(
              To: ticket.Email,
              Subject: "Ticket Rejected",
              HtmlBody: $"<p>Ticket Rejected  With Subject {ticket.Subject} has been rejected and the Reason : {request.RejectReason ?? string.Empty}" +
              $" has been sent to you.</p><p><b>Id:</b> {ticket.Subject} .</p><p><b>reason:</b  : {request.RejectReason ?? string.Empty}</p>"),
          cancellationToken);
            return Result.Ok();
        }
    }
}