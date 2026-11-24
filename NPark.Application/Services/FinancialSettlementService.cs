using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction.Security;
using NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction;
using NPark.Application.Feature.TicketsManagement.Command.CollectEnterTickets;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Services;

public enum SettlementKind { Enter, Exceed, Daily }

public sealed record SettleFinancialRecords(SettlementKind Kind) : ITransactionalCommand<SettlementBatch>;

public sealed record SettlementBatch(TicketCollectionTransaction Transaction, string CollectorName,
    IReadOnlyDictionary<Guid, string> CashierNames, IReadOnlyList<TicketExceedTransaction> Exceeds);

public interface IFinancialSettlementService
{
    Task<Result<SettlementBatch>> SettleAsync(SettlementKind kind, CancellationToken ct);
}

internal sealed class SettleFinancialRecordsHandler(IFinancialSettlementService service)
    : ICommandHandler<SettleFinancialRecords, SettlementBatch>
{
    public Task<Result<SettlementBatch>> Handle(SettleFinancialRecords request, CancellationToken ct) =>
        service.SettleAsync(request.Kind, ct);
}

internal sealed class FinancialSettlementService(IUnitOfWork db, IHttpContextAccessor http, ITokenReader tokens,
    ParkingBusinessClock clock) : IFinancialSettlementService
{
    public async Task<Result<SettlementBatch>> SettleAsync(SettlementKind kind, CancellationToken ct)
    {
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true || principal.GetGateId(tokens) is not Guid gateId ||
            principal.GetUserId(tokens) is not Guid collectorId)
            return Reject("Token.Missing", ErrorMessage.TokenInfo_Missing, ErrorType.Security);
        if (kind != SettlementKind.Daily && !principal.IsInRole("Supervisor"))
            return Reject("User.NotSupervisor", ErrorMessage.Supervisor_OnlyCanCollectTicketExceedTransactions, ErrorType.Security);
        if (kind == SettlementKind.Daily && !principal.GetPermissions().Contains("CollectTickets", StringComparer.OrdinalIgnoreCase))
            return Reject("User.CannotCollect", ErrorMessage.TokenInfo_Missing, ErrorType.Security);

        var user = await db.Repository<User>().GetByIdAsync(collectorId, ct);
        var gate = await db.Repository<ParkingGate>().GetByIdAsync(gateId, ct);
        if (user is null || gate is null)
            return Reject("GateOrUser.NotFound", ErrorMessage.GateNotFound, ErrorType.NotFound);

        var paidAt = clock.LocalNow;
        var mode = gate.GateType == GateType.Entrance ? PriceType.Enter : PriceType.Exit;
        if (kind == SettlementKind.Enter && mode != PriceType.Enter)
            return Reject("Gate.InvalidForEnterCollection", ErrorMessage.GateInvalidForCollectExitPricing);
        if (kind == SettlementKind.Daily)
        {
            var configuration = await db.Repository<ParkingSystemConfiguration>().GetByIdAsync(gate.ParkingSystemConfigurationId, ct);
            if (configuration is null)
                return Reject("Configuration.NotFound", ErrorMessage.Configuration_NotFound, ErrorType.NotFound);
            if (configuration.PriceType != mode)
                return Reject("Gate.InvalidForDailyCollection", ErrorMessage.GateInvalidForCollectExitPricing);
        }

        var type = kind == SettlementKind.Exceed ? TicketCollectionTransactionType.ExceedTransactions :
            mode == PriceType.Enter ? TicketCollectionTransactionType.EnterTickets : TicketCollectionTransactionType.ExitTickets;
        var transaction = TicketCollectionTransaction.Create(type, gateId, collectorId, paidAt);
        List<TicketExceedTransaction> exceeds = [];

        if (kind == SettlementKind.Exceed)
        {
            exceeds = await db.ListWithSpecAsync(new UncollectedTicketExceedTransactionsByGateSpec(gateId), ct);
            foreach (var exceed in exceeds)
            {
                transaction.AddTicket(TicketCollectionTransactionTicket.CreateFromExceedTransaction(transaction.Id, exceed));
                exceed.SetSupervisorCollected(collectorId, paidAt);
            }
        }
        else
        {
            var dayStart = paidAt.Date;
            var dayEnd = dayStart.AddDays(1);
            var utcStart = clock.ToUtc(dayStart);
            var utcEnd = clock.ToUtc(dayEnd);
            var daily = kind == SettlementKind.Daily;
            var enter = mode == PriceType.Enter;
            var live = enter
                ? await db.ListWithSpecAsync(new UncollectedEnterTicketsByGateSpec(gateId), ct)
                : await db.ListWithSpecAsync(new Matching<Ticket>(t => t.BasePaymentGateId == gateId &&
                    t.PricingModeSnapshot == PriceType.Exit && t.IsCashierCollected && t.Price > 0 &&
                    t.CollectedBy == null && !t.IsCollected), ct);
            var archives = enter
                ? await db.ListWithSpecAsync(new UncollectedArchivedEnterTicketsByGateSpec(gateId), ct)
                : await db.ListWithSpecAsync(new Matching<TicketArchive>(t =>
                    (t.BasePaymentGateId ?? t.ExitGateId) == gateId &&
                    (t.PricingModeSnapshot == null || t.PricingModeSnapshot == PriceType.Exit) &&
                    t.IsCashierCollected && t.Price > 0 && t.CollectedBy == null && !t.IsCollected), ct);

            bool OnDay(DateTime? utc, DateTime fallback) => !daily ||
                (utc.HasValue ? utc >= utcStart && utc < utcEnd : fallback >= dayStart && fallback < dayEnd);

            foreach (var ticket in live.Where(t => OnDay(t.BasePaymentConfirmedAtUtc, t.StartDate)))
            {
                if (await db.AnyWithSpecAsync(new Matching<TicketArchive>(x => x.Id == ticket.Id), ct))
                    return Reject("Ticket.DuplicateHistory", "An active ticket also has an archive record.");
                transaction.AddTicket(TicketCollectionTransactionTicket.CreateFromTicket(transaction.Id, ticket));
                ticket.SetCollected(collectorId, paidAt);
            }

            foreach (var ticket in archives.Where(t => OnDay(t.BasePaymentConfirmedAtUtc, enter ? t.StartDate : t.EndDate)))
            {
                transaction.AddTicket(TicketCollectionTransactionTicket.CreateFromTicketArchive(transaction.Id, ticket));
                ticket.SetCollected(collectorId, paidAt);
            }
        }

        if (transaction.Tickets.Count == 0)
            return Reject("Tickets.NoneToCollect", ErrorMessage.Tickets_NoneToCollect);

        transaction.BuildLinesFromTickets();
        var cashierIds = transaction.Lines.Select(x => x.ResponsibleUserId).OfType<Guid>().ToArray();
        var cashiers = await db.ListWithSpecAsync(new Matching<User>(x => cashierIds.Contains(x.Id)), ct);
        await db.Repository<TicketCollectionTransaction>().AddAsync(transaction, ct);
        return Result<SettlementBatch>.Ok(new(transaction, user.Name, cashiers.ToDictionary(x => x.Id, x => x.Name), exceeds));
    }

    private static Result<SettlementBatch> Reject(string code, string message, ErrorType type = ErrorType.Conflict) =>
        Result<SettlementBatch>.Fail(new Error(code, message, type));
}

public sealed class SettlementAudit(IAuditLogger audit, ILogger<SettlementAudit> logger)
{
    public async Task WriteAsync(SettlementBatch batch, SettlementKind kind)
    {
        var transaction = batch.Transaction;
        try
        {
            await audit.LogAsync(new AuditLogEntry(
                EventName: kind switch
                {
                    SettlementKind.Enter => "CollectEnterTicketsSucceeded",
                    SettlementKind.Exceed => "CollectTicketExceedTransactionsSucceeded",
                    _ => "CollectDailyTicketSucceeded"
                },
                EventCategory: "TicketManagement", StatusCode: StatusCodes.Status200OK,
                UserId: transaction.CollectedBy, GateId: transaction.GateId,
                Extra: new
                {
                    CollectionTransactionId = transaction.Id,
                    TransactionId = transaction.Id,
                    SettlementType = transaction.Type.ToString(),
                    LinesCount = transaction.Lines.Count,
                    TicketDetailsCount = transaction.Tickets.Count,
                    CollectedByUserId = transaction.CollectedBy,
                    transaction.GateId,
                    transaction.TotalItemsCollected,
                    transaction.TotalAmountCollected,
                    Cashiers = transaction.Lines.Select(line => line.ResponsibleUserId)
                }), CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Post-commit audit write failed for collection {CollectionTransactionId}", transaction.Id);
        }
    }
}
