using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using NPark.Application.Abstraction.Security;
using NPark.Application.Services;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.ExitTicket;

internal sealed class ExitTicketCommandHandler(GateOperationService operations, IHttpContextAccessor http, ITokenReader tokens)
    : ICommandHandler<ExitTicketCommand, GateOperationResponse>
{
    public Task<Result<GateOperationResponse>> Handle(ExitTicketCommand request, CancellationToken ct)
    {
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true || principal.GetGateId(tokens) is not Guid gateId ||
            principal.GetUserId(tokens) is not Guid userId)
            return Task.FromResult(Result<GateOperationResponse>.Fail(
                new Error("Token.Missing", ErrorMessage.TokenInfo_Missing, ErrorType.Security)));
        if (request.TicketId == Guid.Empty)
            return Task.FromResult(Result<GateOperationResponse>.Fail(new Error(
                "TicketId.Required", ErrorMessage.TicketId_Required, ErrorType.Validation)));
        return operations.AuthorizeAsync(request.TicketId, gateId, null, userId, GateOperationType.Exit, ct, request.RequestId);
    }
}
