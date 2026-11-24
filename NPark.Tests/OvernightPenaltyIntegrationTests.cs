using BuildingBlock.Domain.Results;
using Microsoft.EntityFrameworkCore;
using NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction;
using NPark.Application.Feature.TicketExceedTransactionsManagement.Command.Create;
using NPark.Application.Feature.TicketsManagement.Command.Add;
using NPark.Application.Feature.TicketsManagement.Command.CalculateSalary;
using NPark.Application.Feature.TicketsManagement.Command.CalculateSalaryForSubscriber;
using NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText;
using NPark.Application.Feature.TicketsManagement.Query.ValidateExitByVehicleNumber;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using Xunit;

namespace NPark.Tests;

public sealed partial class ParkingWorkflowIntegrationTests
{
    [SqlFact]
    public async Task OverdueAndSubscriberEligibilityDoesNotDependOnConfigurationOrCatalogAvailability()
    {
        var time = new ManualParkingTime(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
        await using var fixture = await Fixture.StartAsync(time, seedCatalog: false, includeObsoleteSettings: true);
        var data = await fixture.SeedAsync();
        var oldDate = DateOnly.FromDateTime(data.StartDate);

        var overdue = await fixture.SendAsync(new ValidateExitByVehicleNumberQuery("TEST 123"),
            data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.Equal("OvernightPenaltyRequired", overdue.Value.ReasonCode);
        Assert.Equal(0, overdue.Value.Eligibility!.RequiredOvernightAmount);
        var missingCatalog = await fixture.PayAsync(Payment(data, Guid.NewGuid(), 50, "missing"), data);
        Assert.Contains(missingCatalog.Errors, x => x.Code == "Sanction.NotFound");
        await AssertNoRenewalAsync(fixture, data);

        var sanction = Sanction.Create("Any available sanction", "", 15);
        await using (var db = fixture.OpenDb())
        {
            var scheme = PricingScheme.CreateEnterPriceSchema("Test subscription", 20);
            var unpaid = Ticket.Create(data.StartDate, 20, data.EntryGate, data.EntryCashier, oldDate, PriceType.Enter);
            unpaid.ConfirmEntry(fixture.Clock.UtcNow.AddDays(-1));
            unpaid.SetVehicleNumber("TEST UNPAID");
            var future = Ticket.Create(data.StartDate, 20, data.EntryGate, data.EntryCashier,
                fixture.Clock.BusinessDate.AddDays(1), PriceType.Enter);
            future.ConfirmBasePayment(data.EntryCashier, data.EntryGate, fixture.Clock.UtcNow);
            future.ConfirmEntry(fixture.Clock.UtcNow);
            future.SetVehicleNumber("TEST FUTURE");
            db.AddRange(scheme, unpaid, future, sanction);
            await db.SaveChangesAsync();
            db.Add(ParkingMemberships.Create("Test member", "123456", "subscriber-1", "TEST MEMBER",
                "test-card-1", scheme.Id, data.StartDate.AddDays(-1), data.StartDate.AddDays(10)));
            await db.SaveChangesAsync();
        }

        var unpaidEligibility = await fixture.SendAsync(new ValidateExitByVehicleNumberQuery("TEST UNPAID"),
            data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.True(unpaidEligibility.Value.RequiresBasePayment);
        Assert.True(unpaidEligibility.Value.RequiresOvernightPenalty);
        Assert.Contains(unpaidEligibility.Value.FailureReasons, x => x.Code == "BasePaymentRequired");
        Assert.Contains(unpaidEligibility.Value.FailureReasons, x => x.Code == "OvernightPenaltyRequired");
        var futureEligibility = await fixture.SendAsync(new ValidateExitByVehicleNumberQuery("TEST FUTURE"),
            data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.Equal("InvalidExitDate", futureEligibility.Value.ReasonCode);

        time.Advance(TimeSpan.FromDays(-1));
        var issued = await fixture.SendAsync(new AddTicketCommand
        {
            IsSubscriber = true, CardNumber = "test-card-1", VehicleNumber = "TEST MEMBER"
        }, data.EntryGate, data.EntryCashier, "EntranceCashier");
        issued.ThrowIfFailed();
        Assert.Equal(0, issued.Value.Price);
        await using (var db = fixture.OpenDb())
        {
            var memberTicket = await db.Set<Ticket>().SingleAsync(x => x.Id == issued.Value.TicketId);
            memberTicket.ConfirmEntry(fixture.Clock.UtcNow);
            await db.SaveChangesAsync();
        }

        time.Advance(TimeSpan.FromDays(1));
        var quote = await fixture.SendAsync(new CalculateSalaryForSubscriberCommand
        {
            CardNumber = "test-card-1", SanctionIds = [sanction.Id]
        }, data.ExitGate, data.ExitCashier, "ExitCashier");
        quote.ThrowIfFailed();
        Assert.Equal("OvernightPenaltyRequired", quote.Value.ReasonCode);
        Assert.Equal(0, quote.Value.BasePrice);
        Assert.Equal(15, quote.Value.QuotedSanctions);
        Assert.Equal(0, quote.Value.ExceedPrice);
        Assert.False(quote.Value.IsValidForExit);

        var renewed = await fixture.PayAsync(Payment(data, sanction.Id, 15, "subscriber") with
        {
            TicketId = issued.Value.TicketId
        }, data);
        renewed.ThrowIfFailed();
        Assert.Equal(fixture.Clock.BusinessDate, renewed.Value.ValidExitDate);
        var subscriber = await fixture.SendAsync(new ValidateExitByTextCommand { Text = "test-card-1" },
            data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.Equal("Eligible", subscriber.Value.ReasonCode);
    }

    [SqlFact]
    public async Task ConfirmationChecksCatalogAmountsAndRenewsOnlyTheCurrentBusinessDate()
    {
        var time = new ManualParkingTime(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
        await using var fixture = await Fixture.StartAsync(time);
        var data = await fixture.SeedAsync();
        time.Advance(TimeSpan.FromDays(3));
        var zero = Sanction.Create("Zero sanction", "", 0);
        var negative = Sanction.Create("Invalid negative sanction", "", -5);
        await using (var db = fixture.OpenDb())
        {
            db.AddRange(zero, negative);
            await db.SaveChangesAsync();
        }

        var missing = await fixture.PayAsync(Payment(data, Guid.NewGuid(), 0, "missing"), data);
        Assert.Contains(missing.Errors, x => x.Code == "Sanction.NotFound");
        var empty = await fixture.PayAsync(Payment(data, zero.Id, 0, "empty") with { SanctionIds = [] }, data);
        Assert.True(empty.IsFailure);
        var invalidAmount = await fixture.PayAsync(Payment(data, negative.Id, 0, "negative"), data);
        Assert.Contains(invalidAmount.Errors, x => x.Code == "Sanction.InvalidAmount");
        var zeroPayment = await fixture.PayAsync(Payment(data, zero.Id, 0, "zero"), data);
        Assert.Contains(zeroPayment.Errors, x => x.Code == "Payment.AmountMustBePositive");
        var noKey = await fixture.PayAsync(Payment(data, data.OrdinaryId, 25, null), data);
        Assert.Contains(noKey.Errors, x => x.Code == "Payment.IdempotencyKeyRequired");

        var selection = new[] { data.CatalogSanctionId, data.OrdinaryId };
        var quote = await fixture.SendAsync(new CalculateSalaryCommand { TicketId = data.TicketId, SanctionIds = selection },
            data.ExitGate, data.ExitCashier, "ExitCashier");
        quote.ThrowIfFailed();
        Assert.Equal(75, quote.Value.QuotedSanctions);
        Assert.Equal(20, quote.Value.BasePrice);
        Assert.Equal(0, quote.Value.ExceedPrice);
        Assert.Equal(0, quote.Value.RequiredOvernightAmount);
        Assert.Equal(DateOnly.FromDateTime(data.StartDate), quote.Value.ValidExitDate);

        var payment = Payment(data, data.CatalogSanctionId, 75, "selection") with { SanctionIds = selection };
        var missingAmount = await fixture.PayAsync(payment with { ExpectedAmount = null }, data);
        Assert.Contains(missingAmount.Errors, x => x.Code == "Payment.ExpectedAmountRequired");
        await UpdateSanctionAsync(fixture, data.CatalogSanctionId, 60);
        var stale = await fixture.PayAsync(payment, data);
        Assert.Contains(stale.Errors, x => x.Code == "Payment.AmountChanged" && x.Type == ErrorType.Conflict);
        await AssertNoRenewalAsync(fixture, data);

        payment = payment with { ExpectedAmount = 85 };
        var renewed = await fixture.PayAsync(payment, data);
        renewed.ThrowIfFailed();
        Assert.Equal(85, renewed.Value.ExceedAmount);
        Assert.Equal(105, renewed.Value.TotalPriceAfterExceed);
        Assert.Equal(fixture.Clock.BusinessDate, renewed.Value.ValidExitDate);
        await UpdateSanctionAsync(fixture, data.CatalogSanctionId, 99);
        var replay = await fixture.PayAsync(payment with { SanctionIds = selection.Reverse().ToArray() }, data);
        replay.ThrowIfFailed();
        Assert.Equal(renewed.Value.TicketExceedTransactionId, replay.Value.TicketExceedTransactionId);
        Assert.Equal(85, replay.Value.ExceedAmount);
        Assert.Contains((await fixture.PayAsync(payment with { ExpectedAmount = 124 }, data)).Errors,
            x => x.Code == "Payment.IdempotencyConflict");
        Assert.Contains((await fixture.PayAsync(payment with { SanctionIds = [data.OrdinaryId] }, data)).Errors,
            x => x.Code == "Payment.IdempotencyConflict");

        await using var verify = fixture.OpenDb();
        var coverage = await verify.Set<OvernightPenaltyCoverage>().SingleAsync();
        Assert.Equal(fixture.Clock.BusinessDate, coverage.BusinessDate);
        var persisted = await verify.Set<TicketExceedTransaction>().SingleAsync();
        Assert.Equal(SanctionClassification.Overnight, persisted.Classification);
        Assert.Equal(coverage.BusinessDate, persisted.PenaltyFromDate);
        Assert.Equal(coverage.BusinessDate, persisted.PenaltyThroughDate);
        using var amounts = System.Text.Json.JsonDocument.Parse(persisted.AppliedSanctionsJson!);
        var snapshots = amounts.RootElement.EnumerateArray().ToDictionary(
            x => x.GetProperty("SanctionId").GetGuid(), x => x.GetProperty("UnitAmount").GetDecimal());
        Assert.Equal(60, snapshots[data.CatalogSanctionId]);
        Assert.Equal(25, snapshots[data.OrdinaryId]);
    }

    [SqlFact]
    public async Task SameDayOrdinaryPaymentAndSupervisorSettlementDoNotRenewValidity()
    {
        var time = new ManualParkingTime(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
        await using var fixture = await Fixture.StartAsync(time);
        var data = await fixture.SeedAsync();
        time.Advance(TimeSpan.FromDays(-1));
        var validOn = fixture.Clock.BusinessDate;
        var sameDay = await fixture.SendAsync(new ValidateExitByVehicleNumberQuery("TEST 123"),
            data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.Equal("Eligible", sameDay.Value.ReasonCode);
        var request = Payment(data, data.OrdinaryId, 25, null);
        var ordinary = await fixture.PayAsync(request, data);
        ordinary.ThrowIfFailed();
        Assert.Equal(validOn, ordinary.Value.ValidExitDate);
        Assert.Contains((await fixture.PayAsync(request with { IdempotencyKey = "different-same-day-key" }, data)).Errors,
            x => x.Code == "Sanction.AlreadyApplied");

        time.Advance(TimeSpan.FromDays(1));
        var settled = await fixture.SendAsync(new CollectTicketExceedTransactionCommand(),
            data.ExitGate, data.Supervisor, "Supervisor");
        settled.ThrowIfFailed();
        var retry = await fixture.PayAsync(request, data);
        retry.ThrowIfFailed();
        Assert.Equal(ordinary.Value.TicketExceedTransactionId, retry.Value.TicketExceedTransactionId);
        Assert.Equal(validOn, retry.Value.ValidExitDate);
        Assert.Equal(25, retry.Value.ExceedPrice);
        Assert.Contains((await fixture.PayAsync(Payment(data, data.CatalogSanctionId, 50, null), data)).Errors,
            x => x.Code == "Payment.IdempotencyKeyRequired");
        var overdue = await fixture.SendAsync(new ValidateExitByVehicleNumberQuery("TEST 123"),
            data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.Equal("OvernightPenaltyRequired", overdue.Value.ReasonCode);
        await using var db = fixture.OpenDb();
        Assert.False(await db.Set<OvernightPenaltyCoverage>().AnyAsync());
        Assert.Equal(SanctionClassification.Ordinary, (await db.Set<TicketExceedTransaction>().SingleAsync()).Classification);
    }

    [SqlFact]
    public async Task ConcurrentRenewalRequestsWithDifferentSanctionsRecordOnlyOnePayment()
    {
        await using var fixture = await Fixture.StartAsync();
        var data = await fixture.SeedAsync();
        var first = Payment(data, data.OrdinaryId, 25, "concurrent-first");
        var second = Payment(data, data.CatalogSanctionId, 50, "concurrent-second");
        var outcomes = await Task.WhenAll(fixture.PayAsync(first, data), fixture.PayAsync(second, data));
        var success = Assert.Single(outcomes.Where(r => r.IsSuccess));
        var rejected = Assert.Single(outcomes.Where(r => r.IsFailure));
        Assert.Contains(rejected.Errors, e => e.Type == ErrorType.Conflict);

        var staleWithoutDate = (outcomes[0].IsFailure ? first : second) with { ExpectedValidExitDate = null };
        Assert.Contains((await fixture.PayAsync(staleWithoutDate, data)).Errors, e => e.Code == "Payment.ValidityChanged");
        var winner = outcomes[0].IsSuccess ? first : second;
        var replay = await fixture.PayAsync(winner, data);
        replay.ThrowIfFailed();
        Assert.Equal(success.Value.TicketExceedTransactionId, replay.Value.TicketExceedTransactionId);
        await using var db = fixture.OpenDb();
        var payment = await db.Set<TicketExceedTransaction>().SingleAsync();
        var coverage = await db.Set<OvernightPenaltyCoverage>().SingleAsync();
        Assert.Equal(payment.Id, coverage.PaymentId);
        Assert.Equal(payment.ExceedAmount, (await db.Set<Ticket>().SingleAsync()).ExceedPrice);
    }

    [SqlFact]
    public async Task SqlWriteFailuresAndDuplicateCoverageCannotPartiallyRecordRenewal()
    {
        await using var fixture = await Fixture.StartAsync();
        var data = await fixture.SeedAsync();
        var request = Payment(data, data.CatalogSanctionId, 50, "rollback-check");
        await using var db = fixture.OpenDb();

        var failures = new (string Install, string Remove)[]
        {
            ("ALTER TABLE dbo.TicketExceedTransactions ADD CONSTRAINT TestRejectPayment CHECK (ExceedAmount < 1);",
                "ALTER TABLE dbo.TicketExceedTransactions DROP CONSTRAINT TestRejectPayment;"),
            ("ALTER TABLE dbo.OvernightPenaltyCoverages ADD CONSTRAINT TestRejectCoverage CHECK (BusinessDate < '20000101');",
                "ALTER TABLE dbo.OvernightPenaltyCoverages DROP CONSTRAINT TestRejectCoverage;"),
            ("ALTER TABLE dbo.Tickets ADD CONSTRAINT TestRejectTicketUpdate CHECK (ExceedPrice = 0);",
                "ALTER TABLE dbo.Tickets DROP CONSTRAINT TestRejectTicketUpdate;")
        };
        foreach (var failure in failures)
        {
            await db.Database.ExecuteSqlRawAsync(failure.Install);
            try
            {
                Assert.True((await fixture.PayAsync(request, data)).IsFailure);
                await AssertNoRenewalAsync(fixture, data);
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync(failure.Remove);
            }
        }

        var ticket = await db.Set<Ticket>().SingleAsync();
        var sanction = await db.Set<Sanction>().SingleAsync(x => x.Id == data.OrdinaryId);
        var assessment = TicketExceedTransaction.Create(ticket, 0, sanction.Amount,
            data.ExitGate, data.ExitCashier, [sanction], fixture.Clock.UtcNow, fixture.Clock.LocalNow,
            "assessment-fixture", SanctionClassification.Ordinary, null, null, "[]");
        db.Entry(assessment).Property(x => x.PaymentStatus).CurrentValue = SanctionPaymentStatus.Assessed;
        db.Entry(assessment).Property(x => x.PaymentConfirmedAtUtc).CurrentValue = null;
        ticket.AddExceedPrice(sanction.Amount);
        db.Add(assessment);
        await db.SaveChangesAsync();
        var unpaidAssessment = await fixture.SendAsync(new ValidateExitByVehicleNumberQuery("TEST 123"),
            data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.Equal("OvernightPenaltyRequired", unpaidAssessment.Value.ReasonCode);
        Assert.True(unpaidAssessment.Value.HasOutstandingSanctions);
        Assert.Equal(25, unpaidAssessment.Value.OutstandingAmount);

        // Simulate a conflicting historical coverage row on this isolated database.
        db.Add(new OvernightPenaltyCoverage(data.TicketId, fixture.Clock.BusinessDate, assessment.Id));
        await db.SaveChangesAsync();
        var conflict = await fixture.PayAsync(request, data);
        Assert.Contains(conflict.Errors, e => e.Type == ErrorType.Conflict);
        await using var verify = fixture.OpenDb();
        Assert.Equal(assessment.Id, (await verify.Set<TicketExceedTransaction>().SingleAsync()).Id);
        Assert.Equal(assessment.Id, (await verify.Set<OvernightPenaltyCoverage>().SingleAsync()).PaymentId);
        var unchanged = await verify.Set<Ticket>().SingleAsync();
        Assert.Equal(DateOnly.FromDateTime(data.StartDate), unchanged.ValidExitDate);
        Assert.Equal(25, unchanged.ExceedPrice);
    }

    private static CreateTicketExceedTransactionCommand Payment(Seed data, Guid sanctionId, decimal amount, string? key) =>
        new()
        {
            TicketId = data.TicketId,
            SanctionIds = [sanctionId],
            IdempotencyKey = key,
            ExpectedAmount = amount,
            ExpectedValidExitDate = DateOnly.FromDateTime(data.StartDate)
        };

    private static async Task AssertNoRenewalAsync(Fixture fixture, Seed data)
    {
        await using var db = fixture.OpenDb();
        var ticket = await db.Set<Ticket>().SingleAsync(x => x.Id == data.TicketId);
        Assert.Equal(DateOnly.FromDateTime(data.StartDate), ticket.ValidExitDate);
        Assert.Equal(0, ticket.ExceedPrice);
        Assert.False(await db.Set<TicketExceedTransaction>().AnyAsync());
        Assert.False(await db.Set<OvernightPenaltyCoverage>().AnyAsync());
    }

    private static async Task UpdateSanctionAsync(Fixture fixture, Guid id, decimal amount)
    {
        await using var db = fixture.OpenDb();
        var sanction = await db.Set<Sanction>().SingleAsync(x => x.Id == id);
        sanction.Update(sanction.Name, sanction.Description, amount);
        await db.SaveChangesAsync();
    }
}
