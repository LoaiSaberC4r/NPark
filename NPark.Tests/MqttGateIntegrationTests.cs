using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using NPark.Application.Feature.GateManagement.Command.OpenGate;
using NPark.Application.Feature.TicketExceedTransactionsManagement.Command.Create;
using NPark.Application.Feature.TicketsManagement.Command.ExitTicket;
using NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using Xunit;

namespace NPark.Tests;

public sealed partial class ParkingWorkflowIntegrationTests
{
    [SqlBrokerFact]
    public async Task HttpExitTimeoutReleasesReservationsAndOnlyTheFreshMatchingCommandArchivesThePaidTicket()
    {
        var time = new ManualParkingTime(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
        await using var fixture = await Fixture.StartAsync(time, enableMqttNetworkServices: true);
        using var simulator = new MqttFactory().CreateMqttClient();
        var commands = Channel.CreateUnbounded<MqttApplicationMessage>();
        var heartbeats = Channel.CreateUnbounded<MqttApplicationMessage>();
        await SubscribeToCommandsAsync(simulator, fixture.Port, commands.Writer, heartbeats.Writer);
        var seed = await fixture.SeedAsync(sameDay: true, exitController: "1");
        using var cashier = await fixture.CreateHttpClientAsync(seed.ExitGate, seed.ExitCashier);
        var request = new ExitTicketCommand { TicketId = seed.TicketId, RequestId = Guid.NewGuid() };
        var sanction = Sanction.Create("Isolation test sanction", "", 400);
        await using (var setup = fixture.OpenDb())
        {
            setup.Add(sanction);
            await setup.SaveChangesAsync();
        }
        var payment = await fixture.PayAsync(new CreateTicketExceedTransactionCommand
        {
            TicketId = seed.TicketId, IdempotencyKey = "isolated-payment", ExpectedAmount = 400,
            ExpectedValidExitDate = fixture.Clock.BusinessDate, SanctionIds = [sanction.Id]
        }, seed);
        payment.ThrowIfFailed();
        var scan = new ValidateExitByTextCommand { Text = await fixture.GetTicketQrAsync(seed.TicketId) };

        await using (var before = fixture.OpenDb())
            Assert.Empty(await before.Set<GateOperation>().ToListAsync());
        Assert.Equal("7BFFA31A2A3A4A7D", Convert.ToHexString((await ReadFrameAsync(heartbeats.Reader)).PayloadSegment.AsSpan()));
        await fixture.Logs.WaitForAsync(log => log.Contains($"MQTT QR capture subscriber configuration. Host=127.0.0.1, Port={fixture.Port}"));
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscription confirmed"));
        var original = await RequestExitAsync(cashier, request);
        var firstFrame = await ReadFrameAsync(commands.Reader);
        AssertOpen(firstFrame, original, 1);
        await fixture.WaitForStatusAsync(original.OperationId, GateOperationStatus.Published);
        await using (var published = fixture.OpenDb())
        {
            var row = await published.Set<GateOperation>().SingleAsync();
            Assert.Equal((1, seed.TicketId), (row.ReservedControllerId, row.ReservedTicketId));
            output.WriteLine($"SQL before expiry: operation {row.Id}, command {row.CommandId}, status {row.Status}, " +
                $"reserved controller {row.ReservedControllerId}, reserved ticket {row.ReservedTicketId}.");
        }
        time.Advance(TimeSpan.FromSeconds(3));
        await fixture.WaitForStatusAsync(original.OperationId, GateOperationStatus.TimedOut);

        var oldLocation = $"/api/Gate/operations/{original.OperationId}";
        using var historicalResponse = await cashier.GetAsync(oldLocation);
        Assert.Equal(HttpStatusCode.OK, historicalResponse.StatusCode);
        var historical = Assert.IsType<GateOperationResponse>(
            await historicalResponse.Content.ReadFromJsonAsync<GateOperationResponse>());
        Assert.Equal(("TimedOut", 1, original.CommandId, "GateAckTimeout", false),
            (historical.Status, historical.ControllerId, historical.CommandId, historical.ReasonCode, historical.RequiresReconciliation));
        Assert.False(historical.RetryAllowed);
        Assert.False(historical.TicketExited || historical.Archived);
        using var replay = await cashier.PostAsJsonAsync("/api/Ticket/ExitTicket", request);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(original.OperationId, (await replay.Content.ReadFromJsonAsync<GateOperationResponse>())?.OperationId);
        using var deniedRetry = await cashier.PostAsync($"{oldLocation}/retry", null);
        Assert.Equal(HttpStatusCode.Conflict, deniedRetry.StatusCode);
        Assert.Contains("GateFreshRequestRequired", await deniedRetry.Content.ReadAsStringAsync());
        await using (var released = fixture.OpenDb())
        {
            var row = Assert.Single(await released.Set<GateOperation>().ToListAsync());
            Assert.Null(row.ReservedControllerId);
            Assert.Null(row.ReservedTicketId);
            Assert.NotNull(row.DispatchStartedAtUtc);
            Assert.NotNull(row.PublishedAtUtc);
            Assert.Null(row.AcknowledgedAtUtc);
            var ticket = await released.Set<Ticket>().SingleAsync();
            Assert.Equal((seed.TicketId, seed.StartDate, seed.UniqueCode), (ticket.Id, ticket.StartDate, ticket.UniqueCode));
            Assert.Null(ticket.EndDate);
            Assert.Equal(payment.Value.TicketExceedTransactionId, (await released.Set<TicketExceedTransaction>().SingleAsync()).Id);
            Assert.Empty(await released.Set<TicketCollectionTransaction>().ToListAsync());
            output.WriteLine($"SQL after expiry: operation {row.Id}, command {row.CommandId}, status {row.Status}, " +
                "reserved controller NULL, reserved ticket NULL, ticket active.");
        }
        var eligible = await fixture.SendAsync(scan, seed.ExitGate, seed.ExitCashier, "ExitCashier");
        Assert.True(eligible.Value.IsValidForExit);
        Assert.Equal((20m, 400m, 420m, 0m, fixture.Clock.BusinessDate),
            (eligible.Value.Eligibility!.BasePrice, eligible.Value.Eligibility.ExceedPrice,
             eligible.Value.Eligibility.PaidAmount, eligible.Value.OutstandingAmount, eligible.Value.ValidExitDate));
        await fixture.RestartAsync();
        Assert.False(commands.Reader.TryRead(out _));
        using var restartedCashier = await fixture.CreateHttpClientAsync(seed.ExitGate, seed.ExitCashier);
        using var restartedSupervisor = await fixture.CreateHttpClientAsync(seed.ExitGate, seed.Supervisor);
        var operation = await RequestExitAsync(restartedCashier, request with { RequestId = Guid.NewGuid() });
        Assert.NotEqual(original.OperationId, operation.OperationId);
        Assert.NotEqual(original.CommandId, operation.CommandId);
        var command = await ReadFrameAsync(commands.Reader);
        AssertOpen(command, operation, 1);
        await fixture.WaitForStatusAsync(operation.OperationId, GateOperationStatus.Published);
        var ack = MakeAck(command);
        var late = MakeAck(firstFrame);
        await PublishAckAsync(simulator, "ack/network/1", late);
        await fixture.Logs.WaitForAsync(log => log.Contains($"Reason=LateAckForTerminalOperation, ControllerId=1, CommandId={original.CommandId}"));
        late[11] = 1;
        await PublishAckAsync(simulator, "ack/network/1", late);
        await fixture.Logs.WaitForAsync(log => log.Contains($"CommandId={original.CommandId}, AckStatusCode=1, OperationStatus=TimedOut"));
        await using (var active = fixture.OpenDb())
        {
            var row = await active.Set<GateOperation>().SingleAsync(x => x.Id == operation.OperationId);
            Assert.Equal((GateOperationStatus.Published, 1, seed.TicketId), (row.Status, row.ReservedControllerId, row.ReservedTicketId));
            Assert.Empty(await active.Set<TicketArchive>().ToListAsync());
        }

        await PublishAckAsync(simulator, "ack/network/1", ack[..^1]);
        await fixture.Logs.WaitForAsync(log => log.Contains("Reason=MalformedFrame"));
        await PublishAckAsync(simulator, "ack/network/2", ack);
        await fixture.Logs.WaitForAsync(log => log.Contains("Reason=ControllerTopicMismatch"));
        var wrongCommand = ack.ToArray();
        BinaryPrimitives.WriteUInt64BigEndian(wrongCommand.AsSpan(3, 8), ulong.Parse(operation.CommandId) + 100);
        await PublishAckAsync(simulator, "ack/network/1", wrongCommand);
        await fixture.Logs.WaitForAsync(log => log.Contains("Reason=NoMatchingOperation"));
        var wrongController = ack.ToArray();
        BinaryPrimitives.WriteUInt16BigEndian(wrongController.AsSpan(1, 2), 2);
        await PublishAckAsync(simulator, "ack/network/2", wrongController);
        await fixture.Logs.WaitForAsync(log => log.Contains($"Reason=NoMatchingOperation, ControllerId=2, CommandId={operation.CommandId}"));

        await using (var db = fixture.OpenDb())
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER [TestRejectAck] ON [GateOperations] AFTER UPDATE AS " +
                "IF UPDATE([AckStatusCode]) THROW 51011, 'ACK persistence failure probe', 1;");
        }
        await PublishAckAsync(simulator, "ack/network/1", ack);
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK could not be persisted"));
        await using (var blocked = fixture.OpenDb())
        {
            var row = await blocked.Set<GateOperation>().SingleAsync(x => x.Id == operation.OperationId);
            Assert.Equal(GateOperationStatus.Published, row.Status);
            Assert.Null(row.AcknowledgedAtUtc);
            Assert.Empty(await blocked.Set<TicketArchive>().ToListAsync());
            await blocked.Database.ExecuteSqlRawAsync("DROP TRIGGER [TestRejectAck];");
        }

        // The persistent broker session redelivers the original unconfirmed MQTT delivery.
        await fixture.WaitForStatusAsync(operation.OperationId, GateOperationStatus.Completed);
        await PublishAckAsync(simulator, "ack/network/1", ack);
        await fixture.Logs.WaitForAsync(log => log.Contains("Reason=Duplicate"));
        var completed = await restartedCashier.GetFromJsonAsync<GateOperationResponse>($"/api/Gate/operations/{operation.OperationId}");
        Assert.Equal("Completed", completed?.Status);
        Assert.True(completed!.IsAcknowledged && completed.TicketExited && completed.Archived);
        Assert.False(completed.RequiresReconciliation);
        Assert.Equal((byte)0, completed.AckStatusCode);
        await using var after = fixture.OpenDb();
        var saved = await after.Set<GateOperation>().SingleAsync(x => x.Id == operation.OperationId);
        var archive = Assert.Single(await after.Set<TicketArchive>().ToListAsync());
        Assert.Equal(seed.TicketId, archive.Id);
        Assert.Equal(seed.UniqueCode, archive.UniqueCode);
        Assert.Equal(420, archive.TotalPrice);
        Assert.Equal(operation.OperationId, archive.ExitOperationId);
        Assert.False(await after.Set<Ticket>().AnyAsync(x => x.Id == seed.TicketId));
        Assert.Null(saved.ReservedTicketId);
        Assert.Null(saved.ReservedControllerId);
        Assert.NotNull(saved.AcknowledgedAtUtc);
        Assert.Equal((byte)0, saved.AckStatusCode);
        var evidence = new { Evidence = "Isolated controller simulator withheld execution." };
        using var unauthorized = await restartedCashier.PostAsJsonAsync($"{oldLocation}/reconcile-non-execution", evidence);
        Assert.Equal(HttpStatusCode.Forbidden, unauthorized.StatusCode);
        using var reconciliation = await restartedSupervisor.PostAsJsonAsync($"{oldLocation}/reconcile-non-execution", evidence);
        Assert.Equal(HttpStatusCode.OK, reconciliation.StatusCode);
        var annotated = await after.Set<GateOperation>().SingleAsync(x => x.Id == original.OperationId);
        Assert.Equal((GateOperationStatus.TimedOut, "GateAckTimeout", seed.Supervisor),
            (annotated.Status, annotated.ErrorCode, annotated.ReconciledBy));
        Assert.False(commands.Reader.TryRead(out _));
        output.WriteLine($"Isolated broker first command {firstFrame.Topic} | {Convert.ToHexString(firstFrame.PayloadSegment.AsSpan())}");
        output.WriteLine($"Isolated broker fresh command {command.Topic} | {Convert.ToHexString(command.PayloadSegment.AsSpan())}");
        output.WriteLine($"Ignored simulator late ACK ack/network/1 | {Convert.ToHexString(MakeAck(firstFrame))}");
        output.WriteLine($"Simulator ACK ack/network/1 | {Convert.ToHexString(ack)}");
        output.WriteLine($"SQL operation {saved.Id}, ticket {archive.Id}, controller {saved.ControllerId}, command {saved.CommandId}, " +
            $"status {saved.Status}, dispatch {saved.DispatchStartedAtUtc:O}, published {saved.PublishedAtUtc:O}, " +
            $"ACK {saved.AckStatusCode:X2} at {saved.AcknowledgedAtUtc:O}, completed {saved.CompletedAtUtc:O}, archives 1.");
    }

    [SqlBrokerFact]
    public async Task BrokerOutageAndRejectedSubscriptionDeferDispatchAcrossRestartAndReconnect()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var time = new ManualParkingTime(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
        await using var fixture = await Fixture.StartAsync(time, brokerPort: port);
        var seed = await fixture.SeedAsync(sameDay: true, exitController: "1");
        var exit = await fixture.SendAsync(new ExitTicketCommand { TicketId = seed.TicketId },
            seed.ExitGate, seed.ExitCashier, "ExitCashier");
        exit.ThrowIfFailed();
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscription unavailable"));
        await fixture.RestartAsync();

        var acceptSubscription = 0;
        using var broker = new MqttFactory().CreateMqttServer(new MqttServerOptionsBuilder()
            .WithDefaultEndpoint().WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
            .WithDefaultEndpointBoundIPV6Address(IPAddress.IPv6Loopback).WithDefaultEndpointPort(port)
            .WithPersistentSessions().Build());
        broker.InterceptingSubscriptionAsync += args =>
        {
            if (args.TopicFilter.Topic == "ack/network/#" && Volatile.Read(ref acceptSubscription) == 0)
            {
                args.Response.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
                args.ProcessSubscription = false;
            }
            return Task.CompletedTask;
        };
        await broker.StartAsync();
        using var simulator = new MqttFactory().CreateMqttClient();
        var commands = Channel.CreateUnbounded<MqttApplicationMessage>();
        await SubscribeToCommandsAsync(simulator, port, commands.Writer);
        byte[] retained = [0x7B, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0x7D];
        await PublishAckAsync(simulator, "ack/network/1", retained, retain: true);
        await fixture.Logs.WaitForAsync(log => log.Contains("Unusable gate ACK SUBACK"));
        Assert.False(commands.Reader.TryRead(out _));
        await using (var pending = fixture.OpenDb())
        {
            var operation = await pending.Set<GateOperation>().SingleAsync();
            Assert.Equal(exit.Value.OperationId, operation.Id);
            Assert.Equal(GateOperationStatus.Pending, operation.Status);
            Assert.Null(operation.DispatchStartedAtUtc);
        }

        Volatile.Write(ref acceptSubscription, 1);
        await fixture.Logs.WaitForAsync(log => log.Contains("Reason=Retained"));
        var frame = await ReadFrameAsync(commands.Reader);
        AssertOpen(frame, exit.Value, 1);
        await fixture.WaitForStatusAsync(exit.Value.OperationId, GateOperationStatus.Published);
        await PublishAckAsync(simulator, "ack/network/1", MakeAck(frame));
        await fixture.WaitForStatusAsync(exit.Value.OperationId, GateOperationStatus.Completed);
        await broker.StopAsync(new MqttServerStopOptions());
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscriber disconnected") && log.Contains("AckSubscriptionReady=False"));
        var manual = await fixture.SendAsync(new OpenGateCommand { ControllerId = "1", OperationType = "Manual" },
            seed.ExitGate, seed.Supervisor, "Supervisor");
        manual.ThrowIfFailed();
        time.Advance(TimeSpan.FromSeconds(10));
        await fixture.WaitForStatusAsync(manual.Value.OperationId, GateOperationStatus.TimedOut);
        await using (var expired = fixture.OpenDb())
        {
            var row = await expired.Set<GateOperation>().SingleAsync(x => x.Id == manual.Value.OperationId);
            Assert.Equal("GateDispatchTimeout", row.ErrorCode);
            Assert.Null(row.DispatchStartedAtUtc);
            Assert.Null(row.ReservedControllerId);
        }
        await broker.StartAsync();
        using var restartedSimulator = new MqttFactory().CreateMqttClient();
        await SubscribeToCommandsAsync(restartedSimulator, port, commands.Writer);
        var newManual = await fixture.SendAsync(new OpenGateCommand { ControllerId = "1", OperationType = "Manual" },
            seed.ExitGate, seed.Supervisor, "Supervisor");
        newManual.ThrowIfFailed();
        Assert.NotEqual(manual.Value.CommandId, newManual.Value.CommandId);
        var manualFrame = await ReadFrameAsync(commands.Reader);
        AssertOpen(manualFrame, newManual.Value, 1);
        await PublishAckAsync(restartedSimulator, "ack/network/1", MakeAck(manualFrame));
        await fixture.WaitForStatusAsync(newManual.Value.OperationId, GateOperationStatus.Completed);
        Assert.False(commands.Reader.TryRead(out _));
    }

    private static async Task<GateOperationResponse> RequestExitAsync(HttpClient cashier, ExitTicketCommand command)
    {
        using var response = await cashier.PostAsJsonAsync("/api/Ticket/ExitTicket", command);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var operation = Assert.IsType<GateOperationResponse>(await response.Content.ReadFromJsonAsync<GateOperationResponse>());
        Assert.Equal($"/api/Gate/operations/{operation.OperationId}", response.Headers.Location?.OriginalString);
        Assert.False(operation.TicketExited || operation.Archived);
        return operation;
    }

    private static async Task SubscribeToCommandsAsync(IMqttClient simulator, int port,
        ChannelWriter<MqttApplicationMessage> commands, ChannelWriter<MqttApplicationMessage>? heartbeats = null)
    {
        simulator.ApplicationMessageReceivedAsync += args =>
        {
            var message = args.ApplicationMessage;
            if (message.PayloadSegment.Count == 16)
                commands.TryWrite(message);
            else
                heartbeats?.TryWrite(message);
            return Task.CompletedTask;
        };
        await simulator.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", port)
            .WithProtocolVersion(MqttProtocolVersion.V311).WithClientId($"simulator-{Guid.NewGuid():N}").Build());
        await simulator.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("sub/network/#",
            MqttQualityOfServiceLevel.AtLeastOnce).Build());
    }

    private static Task<MqttApplicationMessage> ReadFrameAsync(ChannelReader<MqttApplicationMessage> reader) =>
        reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(15));

    private static void AssertOpen(MqttApplicationMessage command, GateOperationResponse operation, ushort controller)
    {
        Assert.Equal($"sub/network/{controller}", command.Topic);
        Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, command.QualityOfServiceLevel);
        Assert.False(command.Retain);
        Assert.Equal(GatePacketCodec.EncodeOpen(controller, ulong.Parse(operation.CommandId)), command.PayloadSegment.ToArray());
    }

    private static byte[] MakeAck(MqttApplicationMessage command) => MakeAck(command.PayloadSegment.AsSpan());

    private static byte[] MakeAck(ReadOnlySpan<byte> command)
    {
        byte[] ack = [0x7B, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x7D];
        command.Slice(1, 10).CopyTo(ack.AsSpan(1));
        return ack;
    }

    private static async Task PublishAckAsync(IMqttClient simulator, string topic, byte[] ack, bool retain = false)
    {
        var result = await simulator.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(topic)
            .WithPayload(ack).WithRetainFlag(retain).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());
        Assert.True(result.IsSuccess);
    }

    private sealed class MqttTestLogs : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> messages = new();

        public ILogger CreateLogger(string categoryName) => new CaptureLogger(messages, categoryName);
        public void Dispose() { }

        public async Task WaitForAsync(Func<string, bool> predicate)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (!timeout.IsCancellationRequested && !messages.Any(predicate))
                await Task.Delay(100);
            Assert.True(messages.Any(predicate), string.Join(Environment.NewLine, messages.TakeLast(30)));
        }

        private sealed class CaptureLogger(ConcurrentQueue<string> messages, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => category.Contains(".Services.Mqtt") ||
                category.EndsWith(".GateOperationTransitions", StringComparison.Ordinal) ||
                category.EndsWith(".LprCaptureArchiveService", StringComparison.Ordinal);
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel))
                    messages.Enqueue(formatter(state, exception) + " " + exception?.Message);
            }
        }
    }
}
