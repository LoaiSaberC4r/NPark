using System.Globalization;
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
    OvernightPenaltyPolicy overnight, ParkingBusinessClock clock)
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
        var utc = clock.UtcNow;
        var local = clock.ToBusinessTime(utc);
        var day = DateOnly.FromDateTime(local);
        var overnightSelected = overnight.SanctionId is Guid overnightId && ids.Contains(overnightId);
        var key = request.IdempotencyKey is string supplied
            ? "client:" + supplied.Trim()
            : "selection:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                string.Join(",", ids) + (overnightSelected ? day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : ""))));

        var prior = await db.ListWithSpecAsync(new TicketSanctionsSpec(ticket.Id), ct);
        var replay = prior.SingleOrDefault(x => x.IdempotencyKey == key);
        if (replay != null)
        {
            if (replay.PaymentCashierId != cashierId || replay.PaymentGateId != gateId ||
                !ids.SequenceEqual(replay.Sanctions.Select(x => x.Id).Order()))
                return Fail("Payment.IdempotencyConflict", ErrorMessage.TicketExceedTransaction_AlreadyExists);
            return Respond(ticket, replay, prior, ids);
        }

        if (await TicketExitEligibilityService.FindReservationAsync(db, ticket.Id, ct) != null)
            return Result<CreateTicketExceedTransactionCommandResponse>.Fail(TicketExitEligibilityService.AsError("GateOperationPending"));

        var selection = new List<Sanction>();
        foreach (var id in ids)
        {
            var sanction = await db.Repository<Sanction>().GetByIdAsync(id, ct);
            if (sanction is null)
                return Fail("Sanction.NotFound", ErrorMessage.Sanction_NotFound, ErrorType.NotFound);
            if (id != overnight.SanctionId && prior.Any(x => x.Sanctions.Any(s => s.Id == id)))
                return Fail("Sanction.AlreadyApplied", ErrorMessage.TicketExceedTransaction_AlreadyExists);
            selection.Add(sanction);
        }

        var coverage = new OvernightRequirement(null, null, null);
        if (overnightSelected)
        {
            coverage = await overnight.GetRequirementAsync(ticket, day, ct);
            if (coverage.Error != null)
                return Result<CreateTicketExceedTransactionCommandResponse>.Fail(coverage.Error);
            if (coverage.Count == 0)
                return Fail("OvernightPenaltyAlreadyCovered", ErrorMessage.TicketExceedTransaction_AlreadyExists);
        }

        var amounts = selection.Select(x => new
        {
            SanctionId = x.Id,
            UnitAmount = x.Amount,
            Units = x.Id == overnight.SanctionId ? coverage.Count : 1
        }).ToArray();
        if (amounts.Any(x => x.UnitAmount < 0))
            return Fail("Sanction.InvalidAmount", "A sanction has an invalid amount.");
        var amount = amounts.Sum(x => x.UnitAmount * x.Units);
        var classification = !overnightSelected ? SanctionClassification.Ordinary :
            selection.Count == 1 ? SanctionClassification.Overnight : SanctionClassification.Mixed;
        var payment = TicketExceedTransaction.Create(ticket, ticket.ExceedPrice, amount, gateId, cashierId,
            selection, utc, local, key, classification, coverage.From, coverage.Through, JsonSerializer.Serialize(amounts));
        ticket.AddExceedPrice(amount);
        if (coverage.From is DateOnly from && coverage.Through is DateOnly through)
        {
            for (var date = from.DayNumber; date <= through.DayNumber; date++)
                await db.Repository<OvernightPenaltyCoverage>().AddAsync(
                    new(ticket.Id, DateOnly.FromDayNumber(date), payment.Id), ct);
            ticket.ExtendExitValidity(through);
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

    private static Result<CreateTicketExceedTransactionCommandResponse> Fail(
        string code, string message, ErrorType type = ErrorType.Conflict) =>
        Result<CreateTicketExceedTransactionCommandResponse>.Fail(new Error(code, message, type));
}
