using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Infrastructure;
using Xunit;

namespace NPark.Tests;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("NPARK_TEST_SQL") is null)
            Skip = "Requires an isolated SQL Server test instance.";
    }
}

public sealed class LegacyMigrationTests
{
    private const string Baseline = "20260505150257_AddingSuperVisorRecord";

    [SqlFact]
    public async Task ExitCompletionModeMigrationKeepsLegacyOperationsReservationsAndCommandSequence()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NPARK_TEST_SQL"))
            { InitialCatalog = "NParkV2Test_" + Guid.NewGuid().ToString("N") };
        await using var db = new NParkDBContext(new DbContextOptionsBuilder<NParkDBContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        var id = Guid.NewGuid();
        var ticket = Guid.NewGuid();
        var gate = Guid.NewGuid();
        var now = new DateTime(2026, 11, 26, 10, 0, 0, DateTimeKind.Utc);
        try
        {
            await db.Database.MigrateAsync("20261125135033_AddLprAndParkingEventIntegration");
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [GateOperations] ([Id], [TicketIdSnapshot], [ReservedTicketId], [ReservedControllerId],
                    [GateId], [ControllerId], [Type], [Status], [CreatedAtUtc], [CreatedOnUtc], [DispatchStartedAtUtc], [PublishedAtUtc], [RetryCount])
                VALUES ({id}, {ticket}, {ticket}, 54321, {gate}, 54321, {(int)GateOperationType.Exit},
                    {(int)GateOperationStatus.Published}, {now}, {now}, {now}, {now}, 0);
                """);
            var commandId = await db.Database.SqlQuery<long>($"SELECT [CommandId] AS [Value] FROM [GateOperations] WHERE [Id] = {id}")
                .SingleAsync();
            await db.Database.MigrateAsync();

            var legacy = await db.Set<GateOperation>().AsNoTracking().SingleAsync();
            Assert.Equal((id, gate, commandId, GateOperationCompletionMode.Hardware, GateOperationStatus.Published),
                (legacy.Id, legacy.GateId, legacy.CommandId, legacy.CompletionMode, legacy.Status));
            Assert.Equal((ticket, 54321, now),
                (legacy.ReservedTicketId!.Value, legacy.ReservedControllerId!.Value, legacy.PublishedAtUtc!.Value));
            Assert.NotEmpty(legacy.RowVersion);
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());

            var duplicate = new GateOperation(Guid.NewGuid(), ticket, gate, null, 2, GateOperationType.Exit, now,
                completionMode: GateOperationCompletionMode.Software);
            db.Add(duplicate);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Contains("IX_GateOperations_ReservedTicketId", error.InnerException?.Message);
            db.ChangeTracker.Clear();

            db.Add(new GateOperation(Guid.NewGuid(), null, gate, null, 54321, GateOperationType.Manual, now));
            error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Contains("IX_GateOperations_ReservedControllerId", error.InnerException?.Message);
            db.ChangeTracker.Clear();

            var software = new GateOperation(Guid.NewGuid(), Guid.NewGuid(), gate, null, 54321, GateOperationType.Exit, now,
                completionMode: GateOperationCompletionMode.Software);
            db.Add(software);
            await db.SaveChangesAsync();
            Assert.True(software.CommandId > commandId);
            Assert.Null(software.ReservedControllerId);
            Assert.Equal(GateOperationCompletionMode.Software, (await db.Set<GateOperation>().AsNoTracking()
                .SingleAsync(x => x.Id == software.Id)).CompletionMode);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [SqlFact]
    public async Task UpgradesLegacyCashAndSettlementRecordsAndRejectsDuplicateHistory()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NPARK_TEST_SQL"))
            { InitialCatalog = "NParkV2Test_" + Guid.NewGuid().ToString("N") };
        await using var db = new NParkDBContext(new DbContextOptionsBuilder<NParkDBContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.Database.MigrateAsync(Baseline);
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @role uniqueidentifier = NEWID(), @user uniqueidentifier = NEWID(),
                        @gate uniqueidentifier = NEWID(), @ticket uniqueidentifier = NEWID(),
                        @batch uniqueidentifier = NEWID(), @duplicate uniqueidentifier = NEWID(),
                        @sanction uniqueidentifier = NEWID(), @stamp datetime2 = '2026-10-01T23:55:00';
                INSERT Roles (Id, NameEn, NameAr, CreatedOnUtc) VALUES (@role, 'Test', 'Test', @stamp);
                INSERT Users (Id, Name, Username, Email, PasswordHash, NationalId, RoleId, CreatedOnUtc)
                VALUES (@user, 'Test cashier', 'test', 'test@example.test', 'test', '1', @role, @stamp);
                INSERT ParkingSystemConfigurations (EntryGatesCount, ExitGatesCount, PriceType,
                    VehiclePassengerData, PrintType, DateTimeFlag, TicketIdFlag, FeesFlag, CreatedOnUtc)
                VALUES (1, 1, 0, 1, 1, 1, 1, 1, @stamp);
                DECLARE @config int = SCOPE_IDENTITY();
                INSERT ParkingGate (Id, GateNumber, GateType, ParkingSystemConfigurationId, CreatedOnUtc, ControlIp)
                VALUES (@gate, 1, 1, @config, @stamp, '4');
                INSERT Tickets (Id, StartDate, Price, ExceedPrice, CreatedOnUtc, UniqueGuidPart,
                    GateId, UserId, IsCashierCollected, UniqueCode)
                VALUES (@ticket, @stamp, 20, 50, @stamp, 0x01020304, @gate, @user, 1, '01020304');
                INSERT Sanctions (Id, Name, Amount, CreatedOnUtc) VALUES (@sanction, 'Legacy', 50, @stamp);
                INSERT TicketExceedTransactions (Id, TicketIdSnapshot, TicketUniqueCode, TicketStartDate,
                    TicketPrice, TicketOldExceedPrice, TotalPriceAfterExceed, IsSubscriber, TicketGateId, ExceedAmount,
                    CreatedAt, GateId, UserCollectId, CreatedOnUtc)
                VALUES (@ticket, @ticket, '01020304', @stamp, 20, 0, 70, 0, @gate, 50, @stamp, @gate, @user, @stamp);
                INSERT TicketExceedTransactionSanctions (TicketExceedTransactionId, SanctionId) VALUES (@ticket, @sanction);
                INSERT TicketCollectionTransactions (Id, Type, GateId, CollectedBy, CollectedAt, TotalItemsCollected,
                    TotalAmountCollected, CreatedOnUtc)
                VALUES (@batch, 2, @gate, @user, @stamp, 1, 50, @stamp),
                       (@duplicate, 2, @gate, @user, DATEADD(second, 1, @stamp), 1, 50, @stamp);
                INSERT TicketCollectionTransactionTickets (Id, TicketCollectionTransactionId, ResponsibleUserId,
                    Source, TicketIdSnapshot, TicketUniqueCode, TicketStartDate, OriginalPrice, ExceedAmount,
                    AmountCollected, IsSubscriber, TicketGateId, CreatedOnUtc)
                VALUES (NEWID(), @batch, @user, 3, @ticket, '01020304', @stamp, 20, 50, 50, 0, @gate, @stamp),
                       (NEWID(), @duplicate, @user, 3, @ticket, '01020304', @stamp, 20, 50, 50, 0, @gate, @stamp);
                """);

            var probe = new DatabaseStartupTests.StartupProbe();
            using (var host = DatabaseStartupTests.CreateHost(connection.ConnectionString, true, probe))
            {
                var duplicateError = await Assert.ThrowsAsync<SqlException>(() => host.StartAsync());
                Assert.Equal(51002, duplicateError.Number);
                Assert.Contains(probe.Logs, l => l.Level == LogLevel.Error && ReferenceEquals(l.Exception, duplicateError));
                Assert.False(probe.SeedingStarted);
                Assert.False(probe.WorkerStarted);
            }
            Assert.Equal(Baseline, (await db.Database.GetAppliedMigrationsAsync()).Last());
            Assert.Equal(2, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM TicketCollectionTransactionTickets").SingleAsync());

            // Remove just the duplicate inserted by this test, leaving the original legacy history in place.
            await db.Database.ExecuteSqlRawAsync(
                "DELETE item FROM TicketCollectionTransactionTickets item JOIN TicketCollectionTransactions batch ON item.TicketCollectionTransactionId = batch.Id WHERE DATEPART(second, batch.CollectedAt) = 1; DELETE FROM TicketCollectionTransactions WHERE DATEPART(second, CollectedAt) = 1;");
            await db.Database.MigrateAsync();
            var active = await db.Set<Ticket>().SingleAsync();
            var payment = await db.Set<TicketExceedTransaction>().Include(x => x.Sanctions).SingleAsync();
            var item = await db.Set<TicketCollectionTransactionTicket>().SingleAsync();
            Assert.Equal(new DateTime(2026, 10, 1, 23, 55, 0), active.StartDate);
            Assert.Equal(new DateOnly(2026, 10, 1), active.ValidExitDate);
            Assert.Equal(EntryConfirmationStatus.Legacy, active.EntryConfirmationStatus);
            Assert.Null(active.EntryConfirmedAtUtc);
            Assert.Null(active.BasePaymentCashierId);
            Assert.True(active.IsCashierCollected);
            Assert.Equal(active.Id, payment.Id);
            Assert.Null(payment.PaymentConfirmedAtUtc);
            Assert.Single(payment.Sanctions);
            Assert.Equal(payment.Id, item.SourceRecordId);
            Assert.Equal(FinancialItemType.ExceedPayment, item.FinancialItemType);
            Assert.Equal(50, item.AmountCollected);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
