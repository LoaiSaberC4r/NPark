using System.Globalization;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using NPark.Application.Abstraction.Security;
using NPark.Application.Services;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.GateManagement.Command.OpenGate;

internal sealed class OpenGateCommandHandler(GateOperationService operations, IHttpContextAccessor http, ITokenReader tokens)
    : ICommandHandler<OpenGateCommand, GateOperationResponse>
{
    public Task<Result<GateOperationResponse>> Handle(OpenGateCommand request, CancellationToken ct)
    {
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true || principal.GetGateId(tokens) is not Guid gateId ||
            principal.GetUserId(tokens) is not Guid userId)
            return Task.FromResult(Result<GateOperationResponse>.Fail(
                new Error("Token.Missing", ErrorMessage.TokenInfo_Missing, ErrorType.Security)));

        if (!ushort.TryParse(request.ControllerId, NumberStyles.None, CultureInfo.InvariantCulture, out var controller) ||
            controller == 0)
            return Invalid();

        var type = request.OperationType switch
        {
            "Entry" when request.TicketId is Guid id && id != Guid.Empty => GateOperationType.Entry,
            "Manual" when request.TicketId is null => GateOperationType.Manual,
            _ => (GateOperationType?)null
        };
        return type.HasValue
            ? operations.AuthorizeAsync(request.TicketId, gateId, controller, userId, type.Value, ct, request.RequestId)
            : Invalid();
    }

    private static Task<Result<GateOperationResponse>> Invalid() =>
        Task.FromResult(Result<GateOperationResponse>.Fail(new Error("Gate.InvalidRequest",
            "Provide a valid controller and use Entry with a ticket or Manual without a ticket.", ErrorType.Validation)));
}
