using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.QrCode;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using NPark.Application.Abstraction.Security;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.TicketExceedTransactionsManagement.Command.Create;

internal sealed class CreateTicketExceedTransactionCommandHandler(IUnitOfWork db, IQRCodeService qr,
    IByteVerificationService verification, IHttpContextAccessor http, ITokenReader tokens,
    OvernightPenaltyPolicy overnight, ParkingBusinessClock clock, SanctionCatalogSelection catalog)
    : ICommandHandler<CreateTicketExceedTransactionCommand, CreateTicketExceedTransactionCommandResponse>
{
    public async Task<Result<CreateTicketExceedTransactionCommandResponse>> Handle(
        CreateTicketExceedTransactionCommand request, CancellationToken ct)
    {
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true || principal.GetGateId(tokens) is not Guid gateId ||
            principal.GetUserId(tokens) is not Guid cashierId || !principal.IsInRole("ExitCashier"))
            return Fail("Payment.CashierRequired", ErrorMessage.TokenInfo_Missing, ErrorType.Security);
        var gate = await db.Repository<ParkingGate>().GetByIdAsync(gateId, ct);
        if (gate?.GateType != GateType.Exit)
            return Fail("InvalidExitGate", ErrorMessage.GateNotFound, ErrorType.Security);

        var ticket = await db.Repository<Ticket>().GetByIdAsync(request.TicketId, ct);
        if (await db.AnyWithSpecAsync(new Matching<TicketArchive>(x => x.Id == request.TicketId), ct))
            return Result<CreateTicketExceedTransactionCommandResponse>.Fail(TicketExitEligibilityService.AsError("TicketArchived"));
        if (ticket is null)
            return Fail("TicketNotFound", ErrorMessage.Ticket_NotFound, ErrorType.NotFound);
        if (ticket.EndDate.HasValue)
            return Result<CreateTicketExceedTransactionCommandResponse>.Fail(TicketExitEligibilityService.AsError("TicketAlreadyExited"));

        var ids = request.SanctionIds.Order().ToArray();
        var key = request.IdempotencyKey is string supplied
            ? "client:" + supplied.Trim()
            : "selection:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(",", ids))));

        var prior = await db.ListWithSpecAsync(new TicketSanctionsSpec(ticket.Id), ct);
        var replay = prior.SingleOrDefault(x => x.IdempotencyKey == key);
        if (replay is not null)
        {
            if (replay.PaymentStatus != SanctionPaymentStatus.Confirmed ||
                replay.PaymentCashierId != cashierId || replay.PaymentGateId != gateId ||
                !ids.SequenceEqual(replay.Sanctions.Select(x => x.Id).Order()) ||
                request.ExpectedAmount is decimal expected && expected != replay.ExceedAmount)
                return PaymentError("Payment.IdempotencyConflict");

            return Respond(ticket, replay, prior, ids);
        }

        if (await TicketExitEligibilityService.FindReservationAsync(db, ticket.Id, ct) != null)
            return Result<CreateTicketExceedTransactionCommandResponse>.Fail(TicketExitEligibilityService.AsError("GateOperationPending"));

        var utc = clock.UtcNow;
        var local = clock.ToBusinessTime(utc);
        var day = DateOnly.FromDateTime(local);
        var renewsValidity = overnight.RequiresPenalty(ticket, day);
        if (renewsValidity && request.IdempotencyKey is null)
            return PaymentError("Payment.IdempotencyKeyRequired");

        if (request.ExpectedValidExitDate is DateOnly expectedDate && expectedDate != ticket.ValidExitDate)
            return PaymentError("Payment.ValidityChanged");

        // A request that did not submit the observed date cannot be distinguished from
        // a stale renewal once another payment has renewed the ticket today.
        if (!renewsValidity && request.ExpectedValidExitDate is null &&
            await db.AnyWithSpecAsync(new Matching<OvernightPenaltyCoverage>(x =>
                x.TicketIdSnapshot == ticket.Id && x.BusinessDate == day), ct))
            return PaymentError("Payment.ValidityChanged");

        var selected = await catalog.LoadAsync(ids, ct);
        if (selected.IsFailure)
            return Result<CreateTicketExceedTransactionCommandResponse>.Fail(selected.Errors);
        var sanctions = selected.Value;
        if (!renewsValidity && prior.Any(x => x.Sanctions.Any(s => ids.Contains(s.Id))))
            return Fail("Sanction.AlreadyApplied", ErrorMessage.TicketExceedTransaction_AlreadyExists);

        var amount = sanctions.Sum(x => x.Amount);
        if (amount <= 0)
            return PaymentError("Payment.AmountMustBePositive");
        if (request.ExpectedAmount is null)
            return PaymentError("Payment.ExpectedAmountRequired");
        if (request.ExpectedAmount != amount)
            return PaymentError("Payment.AmountChanged");

        var amounts = sanctions.Select(x => new { SanctionId = x.Id, UnitAmount = x.Amount, Units = 1 });
        var classification = renewsValidity ? SanctionClassification.Overnight : SanctionClassification.Ordinary;
        DateOnly? penaltyDate = renewsValidity ? day : null;
        var payment = TicketExceedTransaction.Create(ticket, ticket.ExceedPrice, amount, gateId, cashierId,
            sanctions, utc, local, key, classification, penaltyDate, penaltyDate, JsonSerializer.Serialize(amounts));
        ticket.AddExceedPrice(amount);
        if (renewsValidity)
        {
            await db.Repository<OvernightPenaltyCoverage>().AddAsync(new(ticket.Id, day, payment.Id), ct);
            ticket.ExtendExitValidity(day);
        }
        await db.Repository<TicketExceedTransaction>().AddAsync(payment, ct);
        prior.Add(payment);
        return Respond(ticket, payment, prior, ids);
    }

    private Result<CreateTicketExceedTransactionCommandResponse> Respond(Ticket ticket,
        TicketExceedTransaction payment, IEnumerable<TicketExceedTransaction> history, Guid[] ids)
    {
        var bytes = ticket.UniqueGuidPart.Concat([verification.GenerateComplexByte5FromGuid(ticket.UniqueGuidPart)]).ToArray();
        var qrText = Convert.ToHexString(bytes);
        var paid = (ticket.IsCashierCollected ? ticket.Price : 0) + Math.Min(ticket.ExceedPrice,
            history.Where(x => x.PaymentStatus != SanctionPaymentStatus.Assessed).Sum(x => x.ExceedAmount));
        return Result<CreateTicketExceedTransactionCommandResponse>.Ok(new()
        {
            TicketExceedTransactionId = payment.Id,
            TicketId = ticket.Id,
            ExceedAmount = payment.ExceedAmount,
            ExceedPrice = ticket.ExceedPrice,
            TicketPrice = payment.TicketPrice,
            TotalPriceAfterExceed = payment.TotalPriceAfterExceed,
            PaidAmount = paid,
            OutstandingAmount = Math.Max(0, ticket.TotalPrice - paid),
            ValidExitDate = ticket.ValidExitDate,
            GateId = payment.GateId,
            UserCollectId = payment.UserCollectId,
            SuperVisorCollectorId = payment.SuperVisorCollectorId,
            SanctionIds = ids,
            CreatedAt = payment.CreatedAt,
            PaymentStatus = payment.PaymentStatus.ToString(),
            PaymentConfirmedAtUtc = payment.PaymentConfirmedAtUtc is DateTime at
                ? DateTime.SpecifyKind(at, DateTimeKind.Utc) : null,
            TicketInfo = qrText,
            QrCode = qr.GenerateQRCodeFromText(qrText)
        });
    }

    private static Result<CreateTicketExceedTransactionCommandResponse> PaymentError(string code) =>
        Fail(code, WorkflowMessages.Get(code));

    private static Result<CreateTicketExceedTransactionCommandResponse> Fail(
        string code, string message, ErrorType type = ErrorType.Conflict) =>
        Result<CreateTicketExceedTransactionCommandResponse>.Fail(new Error(code, message, type));
}
