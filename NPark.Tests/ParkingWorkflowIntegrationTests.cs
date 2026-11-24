using System.Buffers.Binary;
using System.Security.Claims;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using NPark.Api.Contracts;
using NPark.Api.Controllers;
using NPark.Application.Abstraction;
using NPark.Application.Abstraction.Security;
using NPark.Application.Feature.MqttNetworkManagement;
using NPark.Application.Feature.MqttNetworkManagement.ProcessMqttExitPacket;
using NPark.Application.Feature.Reports.Query.GetSupervisorCollectionReport;
using NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport;
using NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction;
using NPark.Application.Feature.TicketExceedTransactionsManagement.Command.Create;
using NPark.Application.Feature.TicketsManagement.Command.CollectEnterTickets;
using NPark.Application.Feature.TicketsManagement.Command.CollectDailyTicket;
using NPark.Application.Feature.TicketsManagement.Command.Add;
using NPark.Application.Feature.TicketsManagement.Command.CalculateSalary;
using NPark.Application.Feature.TicketsManagement.Command.ColletByCachier;
using NPark.Application.Feature.GateManagement.Command.OpenGate;
using NPark.Application.Feature.TicketsManagement.Command.ExitTicket;
using NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText;
using NPark.Application.Feature.TicketsManagement.Query.ValidateExitByVehicleNumber;
using NPark.Application.Services;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.FileNames;
using NPark.Infrastructure;
using Xunit;

namespace NPark.Tests;

public sealed class SqlBrokerFactAttribute : FactAttribute
{
    public SqlBrokerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("NPARK_TEST_SQL") is null ||
            Environment.GetEnvironmentVariable("NPARK_TEST_MQTT_PORT") is null)
            Skip = "Requires an isolated SQL Server test instance and loopback MQTT test listener.";
    }
}

public sealed class ParkingWorkflowIntegrationTests
{
    [SqlBrokerFact]
    public async Task OvernightPaymentsSettleIndependentlyAndLateAckArchivesExactlyOnce()
    {
        await using var fixture = await Fixture.StartAsync();
        using var simulator = new MqttFactory().CreateMqttClient();
        var exitFrame = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var publishCount = 0;
        simulator.ApplicationMessageReceivedAsync += message =>
        {
            Interlocked.Increment(ref publishCount);
            exitFrame.TrySetResult(message.ApplicationMessage.PayloadSegment.ToArray());
            return Task.CompletedTask;
        };
        await simulator.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", fixture.Port)
            .WithClientId("test-device-" + Guid.NewGuid().ToString("N")).Build());
        await simulator.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("sub/network/54321",
            MqttQualityOfServiceLevel.AtLeastOnce).Build());

        var data = await fixture.SeedAsync();
        Assert.Equal("OvernightPenaltyRequired", (await fixture.SendAsync(
            new ValidateExitByVehicleNumberQuery("TEST 123"), data.ExitGate, data.ExitCashier, "ExitCashier")).Value.ReasonCode);

        var ordinary = new CreateTicketExceedTransactionCommand
            { TicketId = data.TicketId, SanctionIds = [data.OrdinaryId], IdempotencyKey = "ordinary" };
        var first = await fixture.SendAsync(ordinary, data.ExitGate, data.ExitCashier, "ExitCashier");
        first.ThrowIfFailed();
        Assert.Equal(25, first.Value.ExceedAmount);
        Assert.Equal(45, first.Value.TotalPriceAfterExceed);
        var repeated = await fixture.SendAsync(ordinary, data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.Equal(first.Value.TicketExceedTransactionId, repeated.Value.TicketExceedTransactionId);
        Assert.Equal(DateOnly.FromDateTime(data.StartDate), repeated.Value.ValidExitDate);

        var overnight = new CreateTicketExceedTransactionCommand
            { TicketId = data.TicketId, SanctionIds = [data.OvernightId], IdempotencyKey = "overnight" };
        var second = await fixture.SendAsync(overnight, data.ExitGate, data.ExitCashier, "ExitCashier");
        second.ThrowIfFailed();
        Assert.Equal(50, second.Value.ExceedAmount);
        Assert.Equal(95, second.Value.TotalPriceAfterExceed);
        Assert.Equal(first.Value.TicketInfo, second.Value.TicketInfo);

        var assessed = await fixture.SendAsync(new ValidateExitByTextCommand { Text = second.Value.TicketInfo! },
            data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.True(assessed.Value.IsValidForExit);

        var duplicate = await fixture.SendAsync(overnight with { IdempotencyKey = "another-charge" },
            data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.True(duplicate.IsFailure);

        var sanctions = await fixture.SendAsync(new CollectTicketExceedTransactionCommand(),
            data.ExitGate, data.Supervisor, "Supervisor");
        sanctions.ThrowIfFailed();
        Assert.Equal(2, sanctions.Value.TotalTransactionsCollected);
        Assert.Equal(75, sanctions.Value.TotalExceedAmountCollected);
        Assert.All(sanctions.Value.TicketExceedCollectDetails, item => Assert.Equal(data.ExitCashier, item.ResponsibleUserId));

        var baseCollection = await fixture.SendAsync(new CollectEnterTicketsCommand(),
            data.EntryGate, data.Supervisor, "Supervisor");
        baseCollection.ThrowIfFailed();
        Assert.Equal(20, baseCollection.Value.TotalOriginalPriceCollected);

        var exitRequest = new ExitTicketCommand { TicketId = data.TicketId };
        var qrRequest = new ProcessMqttExitPacketCommand
        {
            DeviceId = "54321",
            PayloadBytes = System.Text.Encoding.ASCII.GetBytes(second.Value.TicketInfo!)
        };
        var httpTask = fixture.ExitHttpAsync(exitRequest, data.ExitGate, data.ExitCashier);
        var qrTask = fixture.SendAsync(qrRequest, data.ExitGate, data.ExitCashier, "ExitCashier");
        await Task.WhenAll(httpTask, qrTask);
        var http = await httpTask;
        var accepted = Assert.IsType<AcceptedResult>(http);
        var pending = Assert.IsType<GateOperationResponse>(accepted.Value);
        Assert.False(pending.TicketExited);
        var qr = await qrTask;
        qr.ThrowIfFailed();
        Assert.Equal(pending.OperationId, qr.Value.OperationId);
        Assert.Equal($"/api/Gate/operations/{pending.OperationId}", accepted.Location);

        var frame = await exitFrame.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(16, frame.Length);
        Assert.Equal(ushort.Parse("54321"), BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(1, 2)));
        var command = BinaryPrimitives.ReadUInt64BigEndian(frame.AsSpan(3, 8));
        Assert.Equal(command.ToString(), pending.CommandId);
        await fixture.WaitForStatusAsync(pending.OperationId, GateOperationStatus.TimedOut);
        await using (var beforeAck = fixture.OpenDb())
        {
            Assert.False(await beforeAck.Set<TicketArchive>().AnyAsync(x => x.Id == data.TicketId));
            Assert.Null((await beforeAck.Set<Ticket>().SingleAsync(x => x.Id == data.TicketId)).EndDate);
        }
        var retry = await fixture.SendAsync(exitRequest, data.ExitGate, data.ExitCashier, "ExitCashier");
        Assert.Equal(pending.OperationId, retry.Value.OperationId);
        Assert.True(retry.Value.RequiresReconciliation);

        byte[] reply = [0x7B, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x7D];
        frame.AsSpan(1, 10).CopyTo(reply.AsSpan(1));
        var ack = new MqttApplicationMessageBuilder().WithTopic("ack/network/54321").WithPayload(reply)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build();
        await using (var failureDb = fixture.OpenDb())
        {
            await failureDb.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER [TestRejectArchive] ON [TicketArchive] AFTER INSERT AS THROW 51010, 'Archive failure probe', 1;");
        }
        await simulator.PublishAsync(ack);
        await simulator.PublishAsync(ack);
        await fixture.WaitForStatusAsync(pending.OperationId, GateOperationStatus.Acknowledged);
        await Task.Delay(1200);
        DateTime? firstAck;
        await using (var failedArchive = fixture.OpenDb())
        {
            Assert.False(await failedArchive.Set<TicketArchive>().AnyAsync(x => x.Id == data.TicketId));
            Assert.Null((await failedArchive.Set<Ticket>().SingleAsync(x => x.Id == data.TicketId)).EndDate);
            firstAck = (await failedArchive.Set<GateOperation>().SingleAsync(x => x.Id == pending.OperationId)).AcknowledgedAtUtc;
            await failedArchive.Database.ExecuteSqlRawAsync("DROP TRIGGER [TestRejectArchive];");
        }

        await fixture.WaitForStatusAsync(pending.OperationId, GateOperationStatus.Completed);
        await using var afterAck = fixture.OpenDb();
        var archive = await afterAck.Set<TicketArchive>().Include(x => x.TicketArchiveAttachments)
            .SingleAsync(x => x.Id == data.TicketId);
        Assert.Equal(data.StartDate, archive.StartDate);
        Assert.Equal(data.UniqueCode, archive.UniqueCode);
        Assert.Equal(pending.OperationId, archive.ExitOperationId);
        Assert.Equal(firstAck, (await afterAck.Set<GateOperation>().SingleAsync(x => x.Id == pending.OperationId)).AcknowledgedAtUtc);
        Assert.Equal(95, archive.TotalPrice);
        Assert.Equal(data.Supervisor, archive.CollectedBy);
        Assert.Single(archive.TicketArchiveAttachments);
        Assert.False(await afterAck.Set<Ticket>().AnyAsync(x => x.Id == data.TicketId));
        Assert.Equal(3, await afterAck.Set<TicketCollectionTransactionTicket>().CountAsync(x => x.TicketIdSnapshot == data.TicketId));
        Assert.Equal(1, Volatile.Read(ref publishCount));
        Assert.True((await fixture.SendAsync(new CollectEnterTicketsCommand(),
            data.EntryGate, data.Supervisor, "Supervisor")).IsFailure);
    }

    [SqlBrokerFact]
    public async Task FailureResultsExceptionsAndNestedFailuresRollBackRealSqlChanges()
    {
        await using var fixture = await Fixture.StartAsync();
        foreach (var failure in new[] { "result", "exception", "nested" })
        {
            var result = await fixture.SendAsync(new RollbackProbe(failure), Guid.NewGuid(), Guid.NewGuid(), "Supervisor");
            Assert.True(result.IsFailure);
        }
        await using var verify = fixture.OpenDb();
        Assert.False(await verify.Set<Sanction>().AnyAsync(x => x.Name == "rollback probe"));
        Assert.True((await fixture.SendAsync(new RollbackProbe("success"), Guid.NewGuid(), Guid.NewGuid(), "Supervisor")).IsSuccess);
        Assert.True(await verify.Set<Sanction>().AnyAsync(x => x.Name == "rollback probe"));
    }

    [SqlBrokerFact]
    public async Task ConfirmedEntryAndExitPricingPreserveTheActualCashierAndReportOnlyTheirSettlement()
    {
        await using var fixture = await Fixture.StartAsync();
        using var simulator = new MqttFactory().CreateMqttClient();
        var frames = System.Threading.Channels.Channel.CreateUnbounded<byte[]>();
        simulator.ApplicationMessageReceivedAsync += args =>
        {
            frames.Writer.TryWrite(args.ApplicationMessage.PayloadSegment.ToArray());
            return Task.CompletedTask;
        };
        await simulator.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", fixture.Port)
            .WithClientId("test-entry-device-" + Guid.NewGuid().ToString("N")).Build());
        await simulator.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("sub/network/+",
            MqttQualityOfServiceLevel.AtLeastOnce).Build());

        var seeded = await fixture.SeedAsync();
        var clock = fixture.Clock;
        var now = clock.LocalNow;
        var businessDate = DateOnly.FromDateTime(now);
        Guid ticketId;
        Guid otherSupervisor;
        await using (var seedDb = fixture.OpenDb())
        {
            var configuration = await seedDb.Set<ParkingSystemConfiguration>().SingleAsync();
            configuration.Update(1, 1, 100, PriceType.Exit, VehiclePassengerData.ScannerId,
                PrintType.QrCode, true, true, true, null, null);
            var scheme = PricingScheme.Create("Test exit hourly", DurationType.Hours, null, null, true, 20, null, 1);
            var additionalSupervisor = Fixture.NewUser("test-supervisor-2", SystemRoles.SupervisorRoleId);
            seedDb.AddRange(scheme, additionalSupervisor);
            await seedDb.SaveChangesAsync();
            seedDb.Add(OrderPricingSchema.Create(scheme.Id, 1));
            await seedDb.SaveChangesAsync();
            otherSupervisor = additionalSupervisor.Id;
        }

        var issued = await fixture.SendAsync(new AddTicketCommand { VehicleNumber = "NP TEST 2" },
            seeded.EntryGate, seeded.EntryCashier, "EntranceCashier");
        issued.ThrowIfFailed();
        ticketId = issued.Value.TicketId;
        Assert.Equal("Pending", issued.Value.EntryConfirmationStatus);
        Assert.Equal(businessDate, issued.Value.ValidExitDate);
        Assert.NotEmpty(issued.Value.QrCode);

        var refused = await fixture.SendAsync(new ExitTicketCommand { TicketId = ticketId },
            seeded.ExitGate, seeded.ExitCashier, "ExitCashier");
        Assert.Contains(refused.Errors, e => e.Code == "EntryNotConfirmed");
        var entry = await fixture.SendAsync(new OpenGateCommand
            { ControllerId = "54320", TicketId = ticketId, OperationType = "Entry" },
            seeded.EntryGate, seeded.EntryCashier, "EntranceCashier");
        entry.ThrowIfFailed();
        await CompleteGateAsync(simulator, await frames.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(12)));
        await fixture.WaitForStatusAsync(entry.Value.OperationId, GateOperationStatus.Completed);

        var unquoted = await fixture.SendAsync(new ColletByCachierCommand { TicketId = ticketId },
            seeded.ExitGate, seeded.ExitCashier, "ExitCashier");
        Assert.True(unquoted.IsFailure);
        var quoteCommand = new CalculateSalaryCommand { TicketId = ticketId, SanctionIds = [seeded.OrdinaryId] };
        var quote = await fixture.SendAsync(quoteCommand, seeded.ExitGate, seeded.ExitCashier, "ExitCashier");
        quote.ThrowIfFailed();
        Assert.Equal(20, quote.Value.BasePrice);
        Assert.Equal(25, quote.Value.QuotedSanctions);
        Assert.Equal(0, quote.Value.ExceedPrice);
        Assert.True((await fixture.SendAsync(new ColletByCachierCommand
            { TicketId = ticketId, ExpectedBasePrice = 22 }, seeded.ExitGate, seeded.ExitCashier, "ExitCashier")).IsFailure);
        Assert.True((await fixture.SendAsync(new ColletByCachierCommand
            { TicketId = ticketId, ExpectedBasePrice = 20 }, seeded.ExitGate, seeded.ExitCashier, "ExitCashier")).IsSuccess);
        var paid = await fixture.SendAsync(quoteCommand, seeded.ExitGate, seeded.ExitCashier, "ExitCashier");
        Assert.Equal(20, paid.Value.BasePrice);
        Assert.Equal(20, paid.Value.PaidAmount);

        var exit = await fixture.SendAsync(new ExitTicketCommand { TicketId = ticketId },
            seeded.ExitGate, seeded.ExitCashier, "ExitCashier");
        exit.ThrowIfFailed();
        await CompleteGateAsync(simulator, await frames.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(12)));
        await fixture.WaitForStatusAsync(exit.Value.OperationId, GateOperationStatus.Completed);
        var collected = await fixture.SendAsync(new CollectDailyTicketCommand(),
            seeded.ExitGate, seeded.Supervisor, "Supervisor");
        collected.ThrowIfFailed();
        Assert.Equal(20, collected.Value.TotalPriceCollected);

        var history = await fixture.SendAsync(new GetSupervisorCollectionReportQuery
        {
            SupervisorId = seeded.Supervisor, FromDate = now.Date, ToDate = now.Date
        }, seeded.ExitGate, seeded.Supervisor, "Supervisor");
        history.ThrowIfFailed();
        var batch = Assert.Single(history.Value.Transactions);
        Assert.Equal(collected.Value.TransactionId, batch.TransactionId);
        var item = Assert.Single(batch.Tickets);
        Assert.Equal(seeded.ExitCashier, item.ResponsibleUserId);
        Assert.Equal(20, history.Value.TotalExitTicketsAmount);
        var other = await fixture.SendAsync(new GetSupervisorCollectionsReportQuery
        {
            SupervisorId = otherSupervisor, From = now.Date, To = now.Date
        }, seeded.ExitGate, otherSupervisor, "Supervisor");
        other.ThrowIfFailed();
        Assert.Equal(0, other.Value.Summary.GrandTotalCollectionsAmount);
        await using var verify = fixture.OpenDb();
        var archive = await verify.Set<TicketArchive>().SingleAsync(x => x.Id == ticketId);
        Assert.Equal(seeded.EntryCashier, archive.UserId);
        Assert.Equal(seeded.ExitCashier, archive.BasePaymentCashierId);
        Assert.Equal(seeded.ExitGate, archive.BasePaymentGateId);
    }

    private static async Task CompleteGateAsync(IMqttClient simulator, byte[] frame)
    {
        var controller = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(1, 2));
        byte[] reply = [0x7B, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x7D];
        frame.AsSpan(1, 10).CopyTo(reply.AsSpan(1));
        await simulator.PublishAsync(new MqttApplicationMessageBuilder().WithTopic($"ack/network/{controller}")
            .WithPayload(reply).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());
    }

    public sealed record RollbackProbe(string Mode) : ITransactionalCommand<int>;

    public sealed class RollbackProbeHandler(IUnitOfWork db, ISender sender) : ICommandHandler<RollbackProbe, int>
    {
        public async Task<Result<int>> Handle(RollbackProbe command, CancellationToken ct)
        {
            await db.Repository<Sanction>().AddAsync(Sanction.Create("rollback probe", "", 1), ct);
            await db.SaveChangesAsync(ct);
            if (command.Mode == "exception")
                throw new InvalidOperationException("Test failure");
            if (command.Mode == "nested")
                _ = await sender.Send(new RollbackProbe("result"), ct);
            return command.Mode == "result"
                ? Result<int>.Fail(new Error("Rollback.Test", "Test failure", ErrorType.Conflict))
                : Result<int>.Ok(1);
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly IHost host;
        private readonly string connection;
        public int Port { get; }
        public ParkingBusinessClock Clock => new(TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(new NPark.Application.Options.ParkingBusinessOptions()));

        private Fixture(IHost host, string connection, int port)
        {
            this.host = host;
            this.connection = connection;
            Port = port;
        }

        public NParkDBContext OpenDb() => OpenDb(connection);

        private static NParkDBContext OpenDb(string connection) =>
            new(new DbContextOptionsBuilder<NParkDBContext>().UseSqlServer(connection).Options);

        public static async Task<Fixture> StartAsync()
        {
            var port = int.Parse(Environment.GetEnvironmentVariable("NPARK_TEST_MQTT_PORT")!);
            var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NPARK_TEST_SQL"))
                { InitialCatalog = "NParkV2Test_" + Guid.NewGuid().ToString("N") };
            var sql = builder.ConnectionString;
            await using var db = OpenDb(sql);
            await db.Database.MigrateAsync();

            var sanction = Sanction.Create("Test overnight", "", 50);
            db.Add(sanction);
            await db.SaveChangesAsync();

            var hostBuilder = Host.CreateApplicationBuilder();
            hostBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = sql,
                ["Mqtt:Host"] = "127.0.0.1",
                ["Mqtt:Port"] = port.ToString(),
                ["Mqtt:ClientId"] = builder.InitialCatalog,
                ["Mqtt:UseTls"] = "false",
                ["MqttHeartbeat:Enabled"] = "false",
                ["MqttNetworkSubscriber:Enabled"] = "false",
                ["GateOperation:RecoveryIntervalSeconds"] = "1",
                ["GateOperation:AckTimeoutSeconds"] = "2",
                ["OvernightParking:OvernightSanctionId"] = sanction.Id.ToString()
            });
            hostBuilder.Services.AddHttpContextAccessor();
            NPark.Application.Bootstrap.Bootstrap.AddApplicationBootstrap(hostBuilder.Services);
            NPark.Infrastructure.Bootstrap.Bootstrap.InfrastructureInjection(hostBuilder.Services, hostBuilder.Configuration);
            hostBuilder.Services.AddSingleton<IRealtimeNotifier, TestNotifications>();
            hostBuilder.Services.AddSingleton<IMqttCaptureSignalRNotifier, TestNotifications>();
            hostBuilder.Services.AddTransient<IRequestHandler<RollbackProbe, Result<int>>, RollbackProbeHandler>();
            var host = hostBuilder.Build();
            await host.StartAsync();
            return new(host, sql, port);
        }

        public async Task<T> SendAsync<T>(IRequest<T> request, Guid gate, Guid user, string role)
        {
            await using var scope = host.Services.CreateAsyncScope();
            var http = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            http.HttpContext = Context(gate, user, role);
            try { return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request); }
            finally { http.HttpContext = null; }
        }

        public async Task<IActionResult> ExitHttpAsync(ExitTicketCommand request, Guid gate, Guid user)
        {
            await using var scope = host.Services.CreateAsyncScope();
            var http = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            http.HttpContext = Context(gate, user, "ExitCashier");
            try
            {
                var controller = new TicketController(scope.ServiceProvider.GetRequiredService<ISender>())
                    { ControllerContext = new() { HttpContext = http.HttpContext } };
                return await controller.ExitTicket(request, CancellationToken.None);
            }
            finally { http.HttpContext = null; }
        }

        private static HttpContext Context(Guid gate, Guid user, string role)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(JwtClaimTypesCustom.GateId, gate.ToString()),
                new Claim(JwtClaimTypesCustom.UserId, user.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtClaimTypesCustom.Permission, "CollectTickets")
            ], "Integration"));
            return new DefaultHttpContext { User = principal };
        }

        public async Task<Seed> SeedAsync()
        {
            await using var db = OpenDb();
            var configuration = ParkingSystemConfiguration.Create(1, 1, 100, PriceType.Enter,
                VehiclePassengerData.ScannerId, PrintType.QrCode, true, true, true, null, null);
            db.Add(configuration);
            await db.SaveChangesAsync();
            var entry = ParkingGate.Create(1, GateType.Entrance, configuration.Id, controlIp: "54320");
            var exit = ParkingGate.Create(2, GateType.Exit, configuration.Id, controlIp: "54321");
            var entryCashier = NewUser("test-entry", SystemRoles.EntranceCashierRoleId);
            var exitCashier = NewUser("test-exit", SystemRoles.ExitCashierRoleId);
            var supervisor = NewUser("test-supervisor", SystemRoles.SupervisorRoleId);
            var ordinary = Sanction.Create("Test ordinary", "", 25);
            db.AddRange(entry, exit, entryCashier, exitCashier, supervisor, ordinary);
            await db.SaveChangesAsync();
            using var scope = host.Services.CreateScope();
            var clock = scope.ServiceProvider.GetRequiredService<ParkingBusinessClock>();
            var start = clock.LocalNow.Date.AddDays(-1).AddHours(10);
            var ticket = Ticket.Create(start, 20, entry.Id, entryCashier.Id, DateOnly.FromDateTime(start), PriceType.Enter);
            ticket.SetVehicleNumber("TEST 123");
            ticket.AddAttachment("existing-test-attachment.jpg");
            ticket.ConfirmBasePayment(entryCashier.Id, entry.Id, clock.UtcNow.AddDays(-1));
            ticket.ConfirmEntry(clock.UtcNow.AddDays(-1));
            db.Add(ticket);
            await db.SaveChangesAsync();
            var overnight = await db.Set<Sanction>().SingleAsync(x => x.Name == "Test overnight");
            return new(entry.Id, exit.Id, entryCashier.Id, exitCashier.Id, supervisor.Id, ticket.Id, ticket.UniqueCode, start, ordinary.Id, overnight.Id);
        }

        public async Task WaitForStatusAsync(Guid id, GateOperationStatus status)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (!timeout.IsCancellationRequested)
            {
                await using var db = OpenDb();
                var operation = await db.Set<GateOperation>().SingleAsync(x => x.Id == id);
                if (operation.Status == status)
                    return;
                await Task.Delay(150);
            }
            await using var verify = OpenDb();
            Assert.Equal(status, (await verify.Set<GateOperation>().SingleAsync(x => x.Id == id)).Status);
        }

        internal static User NewUser(string name, Guid role)
        {
            var user = User.Create(name, name + "@example.test", name, "test", name, name);
            user.SetRole(role);
            return user;
        }

        public async ValueTask DisposeAsync()
        {
            await host.StopAsync();
            if (host is IAsyncDisposable asyncHost)
                await asyncHost.DisposeAsync();
            else
                host.Dispose();
            await using var db = OpenDb();
            await db.Database.EnsureDeletedAsync();
        }
    }

    private sealed record Seed(Guid EntryGate, Guid ExitGate, Guid EntryCashier, Guid ExitCashier, Guid Supervisor,
        Guid TicketId, string UniqueCode, DateTime StartDate, Guid OrdinaryId, Guid OvernightId);

    private sealed class TestNotifications : IRealtimeNotifier, IMqttCaptureSignalRNotifier
    {
        public Task NotifyGateOperationAsync(GateOperationResponse payload) => Task.CompletedTask;
        public Task NotifyTicketAddedAsync(TicketAddedNotification payload) => Task.CompletedTask;
        public Task NotifyTicketExitedAsync(TicketExitedNotification payload) => Task.CompletedTask;
        public Task NotifyCaptureDetectedAsync(MqttCaptureDetectedResponse payload, CancellationToken ct) => Task.CompletedTask;
    }
}

internal static class TestResultExtensions
{
    public static void ThrowIfFailed<T>(this Result<T> result) =>
        Assert.True(result.IsSuccess, result.IsSuccess ? null : string.Join("; ", result.Errors.Select(e => e.Code + " " + e.Message)));
}
