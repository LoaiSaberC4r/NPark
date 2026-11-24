using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Services;

public sealed record ExitEligibility(
    bool IsValidForExit, string ReasonCode, DateOnly? ValidExitDate = null,
    bool RequiresOvernightPenalty = false, decimal BasePrice = 0, decimal ExceedPrice = 0,
    decimal PaidAmount = 0, decimal OutstandingAmount = 0, Guid? OperationId = null,
    bool RequiresBasePayment = false,
    bool HasOutstandingSanctions = false, [property: System.Text.Json.Serialization.JsonIgnore] IReadOnlyList<ExitEligibilityFailure>? Reasons = null)
{
    public string ReasonMessage => WorkflowMessages.Get(ReasonCode);
    public IReadOnlyList<ExitEligibilityFailure> FailureReasons => IsValidForExit ? [] :
        Reasons ?? [new(ReasonCode, ReasonMessage)];
    public Guid? ExistingOperationId => OperationId;
    public decimal RequiredOvernightAmount => 0;
    public decimal TotalPrice => BasePrice + ExceedPrice;
}

public sealed record ExitEligibilityFailure(string Code, string Message);

public abstract record ExitValidationResponse
{
    public Guid? TicketId { get; init; }
    public string? VehicleNumber { get; init; }
    public DateTime? StartDate { get; init; }
    public bool IsValidForExit { get; init; }
    public string ReasonCode { get; init; } = "TicketNotFound";
    public ExitEligibility? Eligibility { get; init; }
    public string ReasonMessage => WorkflowMessages.Get(ReasonCode);
    public IReadOnlyList<ExitEligibilityFailure> FailureReasons =>
        Eligibility?.FailureReasons ?? (IsValidForExit ? [] : [new(ReasonCode, ReasonMessage)]);
    public DateOnly? ValidExitDate => Eligibility?.ValidExitDate;
    public bool RequiresBasePayment => Eligibility?.RequiresBasePayment ?? false;
    public bool RequiresOvernightPenalty => Eligibility?.RequiresOvernightPenalty ?? false;
    public bool HasOutstandingSanctions => Eligibility?.HasOutstandingSanctions ?? false;
    public decimal OutstandingAmount => Eligibility?.OutstandingAmount ?? 0;
    public Guid? ExistingOperationId => Eligibility?.ExistingOperationId;
}

public interface ITicketExitEligibilityService
{
    Task<ExitEligibility> EvaluateAsync(Ticket? ticket, Guid? gateId, CancellationToken ct);
}

public sealed class TicketExitEligibilityService(
    IUnitOfWork db, ParkingBusinessClock clock, OvernightPenaltyPolicy overnight) : ITicketExitEligibilityService
{
    public async Task<ExitEligibility> EvaluateAsync(Ticket? ticket, Guid? gateId, CancellationToken ct)
    {
        if (ticket is null)
            return new(false, "TicketNotFound");

        var payment = await GetBalanceAsync(db, ticket, ct);
        var businessDate = clock.BusinessDate;
        var requiresOvernightPenalty = overnight.RequiresPenalty(ticket, businessDate);
        var operation = await FindReservationAsync(db, ticket.Id, ct);
        var gate = gateId is Guid id ? await db.Repository<ParkingGate>().GetByIdAsync(id, ct) : null;
        var archived = await db.AnyWithSpecAsync(new Matching<TicketArchive>(x => x.Id == ticket.Id), ct);
        var failures = new List<ExitEligibilityFailure>();
        void Add(bool condition, string code)
        {
            if (condition)
                failures.Add(new(code, WorkflowMessages.Get(code)));
        }

        var unpaidSanction = payment.Outstanding > (ticket.IsCashierCollected ? 0 : ticket.Price);
        Add(archived, "TicketArchived");
        Add(ticket.EndDate.HasValue, "TicketAlreadyExited");
        Add(gateId.HasValue && gate is null, "GateNotFound");
        Add(gate != null && gate.GateType != GateType.Exit, "InvalidExitGate");
        Add(operation != null, "GateOperationPending");
        Add(ticket.EntryConfirmationStatus == EntryConfirmationStatus.Pending, "EntryNotConfirmed");
        Add(!ticket.IsCashierCollected, "BasePaymentRequired");
        Add(requiresOvernightPenalty, "OvernightPenaltyRequired");
        Add(ticket.ValidExitDate > businessDate, "InvalidExitDate");
        Add(unpaidSanction, "SanctionPaymentRequired");

        return new(failures.Count == 0, failures.FirstOrDefault()?.Code ?? "Eligible",
            ticket.ValidExitDate, requiresOvernightPenalty, ticket.Price, ticket.ExceedPrice,
            payment.Paid, payment.Outstanding, operation?.Id,
            !ticket.IsCashierCollected, unpaidSanction, failures);
    }

    public static Task<GateOperation?> FindReservationAsync(IUnitOfWork db, Guid ticketId, CancellationToken ct) =>
        db.FirstOrDefaultWithSpecAsync(new Matching<GateOperation>(x => x.ReservedTicketId == ticketId), ct);

    public static async Task<(decimal Paid, decimal Outstanding)> GetBalanceAsync(
        IUnitOfWork db, Ticket ticket, CancellationToken ct)
    {
        var payments = await db.ListWithSpecAsync(new Matching<TicketExceedTransaction>(x =>
            x.TicketIdSnapshot == ticket.Id && x.PaymentStatus != SanctionPaymentStatus.Assessed), ct);
        var paidSanctions = Math.Min(ticket.ExceedPrice, payments.Sum(x => x.ExceedAmount));
        var paid = paidSanctions + (ticket.IsCashierCollected ? ticket.Price : 0);
        return (paid, Math.Max(0, ticket.TotalPrice - paid));
    }

    public static Error AsError(string reason) => new(reason, reason switch
    {
        "TicketNotFound" => ErrorMessage.Ticket_NotFound,
        "GateNotFound" or "InvalidExitGate" => ErrorMessage.GateNotFound,
        _ => WorkflowMessages.Get(reason)
    }, reason == "TicketNotFound" ? ErrorType.NotFound : ErrorType.Conflict);
}

public sealed class Matching<T> : Specification<T> where T : class
{
    public Matching(System.Linq.Expressions.Expression<Func<T, bool>> criteria, int? limit = null)
    {
        AddCriteria(criteria);
        if (limit.HasValue)
            ApplyPaging(1, limit.Value);
    }
}
