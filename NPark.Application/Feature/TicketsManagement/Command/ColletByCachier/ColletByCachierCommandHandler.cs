using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using NPark.Application.Abstraction.Security;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketsManagement.Command.ColletByCachier;

public sealed class ColletByCachierCommandHandler(IUnitOfWork db, IHttpContextAccessor http,
    ITokenReader tokens, ParkingBusinessClock clock) : ICommandHandler<ColletByCachierCommand>
{
    public async Task<Result> Handle(ColletByCachierCommand request, CancellationToken ct)
    {
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true || principal.GetGateId(tokens) is not Guid gateId ||
            principal.GetUserId(tokens) is not Guid cashierId)
            return Result.Fail(new Error("Token.Missing", ErrorMessage.TokenInfo_Missing, ErrorType.Security));

        var ticket = await db.Repository<Ticket>().GetByIdAsync(request.TicketId, ct);
        if (ticket is null)
            return Result.Fail(TicketExitEligibilityService.AsError("TicketNotFound"));

        var gate = await db.Repository<ParkingGate>().GetByIdAsync(gateId, ct);
        if (gate is null)
            return Result.Fail(new Error("Gate.NotFound", ErrorMessage.GateNotFound, ErrorType.NotFound));
        var mode = ticket.PricingModeSnapshot ??
            (await db.Repository<ParkingSystemConfiguration>().GetByIdAsync(gate.ParkingSystemConfigurationId, ct))?.PriceType;
        if (!mode.HasValue)
            return Result.Fail(new Error("Configuration.NotFound", ErrorMessage.Configuration_NotFound, ErrorType.NotFound));

        var isEntry = mode == PriceType.Enter;
        if (gate.GateType != (isEntry ? GateType.Entrance : GateType.Exit) ||
            !principal.IsInRole(isEntry ? "EntranceCashier" : "ExitCashier") ||
            isEntry && ticket.GateId != gateId)
            return Result.Fail(new Error("Gate.InvalidForBasePayment", ErrorMessage.GateNotFound, ErrorType.Security));

        if (ticket.EndDate.HasValue)
            return Result.Fail(TicketExitEligibilityService.AsError("TicketAlreadyExited"));
        if (request.ExpectedBasePrice is decimal expected && expected != ticket.Price)
            return Result.Fail(new Error("BaseQuoteChanged", "The confirmed amount must match the current base quote.", ErrorType.Conflict));
        if (ticket.IsCashierCollected)
            return Result.Ok();
        if (await TicketExitEligibilityService.FindReservationAsync(db, ticket.Id, ct) is { })
            return Result.Fail(TicketExitEligibilityService.AsError("GateOperationPending"));
        if (!ticket.BasePriceCalculated && !isEntry)
            return Result.Fail(new Error("BaseQuoteRequired", "Calculate the base fee before confirming payment.", ErrorType.Conflict));

        ticket.ConfirmBasePayment(cashierId, gateId, clock.UtcNow);
        return Result.Ok();
    }
}
