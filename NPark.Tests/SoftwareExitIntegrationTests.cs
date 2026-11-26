using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Protocol;
using NPark.Application.Abstraction;
using NPark.Application.Feature.TicketExceedTransactionsManagement.Command.CollectTicketExceedTransaction;
using NPark.Application.Feature.TicketExceedTransactionsManagement.Command.Create;
using NPark.Application.Feature.TicketsManagement.Command.CollectEnterTickets;
using NPark.Application.Feature.TicketsManagement.Command.ExitTicket;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using Xunit;

namespace NPark.Tests;

public sealed partial class ParkingWorkflowIntegrationTests
{
    [SqlFact]
    public async Task SoftwareRestExitCommitsOnceWithOfflineMqttAndABusyController()
    {
        await using var fixture = await Fixture.StartAsync(EntranceTime(), brokerPort: 1,
            entranceSettings: SoftwareExitSettings());
        var seed = await fixture.SeedAsync(sameDay: true);
        var busy = new GateOperation(Guid.NewGuid(), null, seed.ExitGate, seed.Supervisor, 54321,
            GateOperationType.Manual, fixture.Clock.UtcNow);
        await using (var db = fixture.OpenDb())
        {
            db.Add(busy);
            await db.SaveChangesAsync();
        }
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscription unavailable"));
        var committedAtNotification = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Notifications.OnExitAsync = async exit =>
        {
            await using var db = fixture.OpenDb();
            var archive = await db.Set<TicketArchive>().FindAsync(exit.TicketId);
            var operation = archive?.ExitOperationId is Guid id ? await db.Set<GateOperation>().FindAsync(id) : null;
            committedAtNotification.TrySetResult(operation?.Status == GateOperationStatus.Completed);
        };

        using var cashier = await fixture.CreateHttpClientAsync(seed.ExitGate, seed.ExitCashier);
        using var anonymous = new HttpClient { BaseAddress = cashier.BaseAddress };
        var first = new ExitTicketCommand { TicketId = seed.TicketId, RequestId = Guid.NewGuid() };
        using var unauthorized = await anonymous.PostAsJsonAsync("/api/Ticket/ExitTicket", first);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        var second = first with { RequestId = Guid.NewGuid() };
        var attempts = await Task.WhenAll(ExitAsync(first), ExitAsync(second));
        var completed = attempts[0];
        Assert.Equal(completed, attempts[1]);
        Assert.Contains(completed.OperationId, new[] { first.RequestId!.Value, second.RequestId!.Value });
        var winningRequest = first with { RequestId = completed.OperationId };
        Assert.Equal(completed, await ExitAsync(winningRequest));
        using var conflicted = await cashier.PostAsJsonAsync("/api/Ticket/ExitTicket",
            winningRequest with { TicketId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Conflict, conflicted.StatusCode);
        Assert.Contains("GateRequestIdConflict", await conflicted.Content.ReadAsStringAsync());

        AssertSoftwareResponse(completed);
        Assert.Equal(fixture.Clock.UtcNow, completed.CompletedAtUtc);
        Assert.Equal(DateTimeKind.Utc, completed.CompletedAtUtc!.Value.Kind);
        Assert.True(await committedAtNotification.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(completed, Assert.Single(fixture.Notifications.Operations, op => op.OperationType == "Exit"));
        var exit = Assert.Single(fixture.Notifications.Exited);
        Assert.Equal((seed.TicketId, seed.ExitGate, seed.ExitCashier, fixture.Clock.LocalNow),
            (exit.TicketId, exit.GateId, exit.UserId, exit.ExitDate));

        var ack = new RecordGateAcknowledgment(new GateAck(54321, ulong.Parse(completed.CommandId), 0));
        var late = await fixture.SendAsync(ack, seed.ExitGate, seed.ExitCashier, "ExitCashier");
        late.ThrowIfFailed();
        Assert.Null(late.Value.Operation);

        await using var verified = fixture.OpenDb();
        var row = await verified.Set<GateOperation>().SingleAsync(x => x.Type == GateOperationType.Exit);
        Assert.Equal(GateOperationCompletionMode.Software, row.CompletionMode);
        Assert.Equal(GateOperationStatus.Completed, row.Status);
        Assert.Null(row.ReservedControllerId);
        Assert.Null(row.ReservedTicketId);
        Assert.Null(row.DispatchStartedAtUtc);
        Assert.Null(row.PublishedAtUtc);
        Assert.Null(row.AcknowledgedAtUtc);
        Assert.Null(row.AckStatusCode);
        Assert.Equal(54321, (await verified.Set<GateOperation>().FindAsync(busy.Id))!.ReservedControllerId);
        Assert.Empty(await verified.Set<Ticket>().ToListAsync());
        var archive = Assert.Single(await verified.Set<TicketArchive>().Include(x => x.TicketArchiveAttachments).ToListAsync());
        Assert.Equal((seed.TicketId, seed.StartDate, seed.UniqueCode, completed.OperationId, fixture.Clock.LocalNow),
            (archive.Id, archive.StartDate, archive.UniqueCode, archive.ExitOperationId!.Value, archive.EndDate));
        Assert.Single(archive.TicketArchiveAttachments);
        Assert.Equal(seed.EntryCashier, archive.BasePaymentCashierId);

        async Task<GateOperationResponse> ExitAsync(ExitTicketCommand command)
        {
            using var response = await cashier.PostAsJsonAsync("/api/Ticket/ExitTicket", command);
            var json = await response.Content.ReadAsStringAsync();
            output.WriteLine($"Software exit HTTP {(int)response.StatusCode}: {json}");
            Assert.True(response.StatusCode == HttpStatusCode.OK, json);
            return Assert.IsType<GateOperationResponse>(await response.Content.ReadFromJsonAsync<GateOperationResponse>());
        }
    }

    [SqlFact]
    public async Task EligibilityRenewalArchiveRollbackAndSupervisorSettlementUseExistingFinancialRecords()
    {
        await using var fixture = await Fixture.StartAsync(EntranceTime(), brokerPort: 1, failNotifications: true,
            entranceSettings: SoftwareExitSettings());
        var seed = await fixture.SeedAsync();
        var pending = await fixture.CreateTicketAsync(seed, 20, confirmEntry: false);
        var sanctionDue = await fixture.CreateTicketAsync(seed, 20, confirmEntry: true);
        var unpaid = Ticket.Create(fixture.Clock.LocalNow, 20, seed.EntryGate, seed.EntryCashier,
            fixture.Clock.BusinessDate, PriceType.Enter);
        unpaid.ConfirmEntry(fixture.Clock.UtcNow);
        await using (var db = fixture.OpenDb())
        {
            (await db.Set<Ticket>().FindAsync(sanctionDue.Id))!.AddExceedPrice(25);
            db.Add(unpaid);
            await db.SaveChangesAsync();
        }

        await AssertBlockedAsync(unpaid.Id, "BasePaymentRequired");
        await AssertBlockedAsync(sanctionDue.Id, "SanctionPaymentRequired");
        await AssertBlockedAsync(pending.Id, "EntryNotConfirmed");
        await AssertBlockedAsync(seed.TicketId, "InvalidExitGate", seed.EntryGate);
        await AssertBlockedAsync(seed.TicketId, "OvernightPenaltyRequired");

        var payment = await fixture.PayAsync(new CreateTicketExceedTransactionCommand
        {
            TicketId = seed.TicketId,
            SanctionIds = [seed.CatalogSanctionId],
            IdempotencyKey = "software-exit-overnight-renewal",
            ExpectedAmount = 50,
            ExpectedValidExitDate = DateOnly.FromDateTime(seed.StartDate)
        }, seed);
        payment.ThrowIfFailed();
        Assert.Equal(fixture.Clock.BusinessDate, payment.Value.ValidExitDate);
        var request = new ExitTicketCommand { TicketId = seed.TicketId, RequestId = Guid.NewGuid() };
        await using (var db = fixture.OpenDb())
        {
            Assert.Empty(await db.Set<GateOperation>().ToListAsync());
            Assert.Empty(await db.Set<TicketArchive>().ToListAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [TicketArchive] ADD CONSTRAINT [TestRejectSoftwareArchive] CHECK ([Price] <> 20);");
        }
        var failed = await fixture.SendAsync(request, seed.ExitGate, seed.ExitCashier, "ExitCashier");
        Assert.True(failed.IsFailure);
        Assert.Empty(fixture.Notifications.Exited);
        Assert.Empty(fixture.Notifications.Operations);
        await using (var db = fixture.OpenDb())
        {
            Assert.Empty(await db.Set<GateOperation>().ToListAsync());
            Assert.Empty(await db.Set<TicketArchive>().ToListAsync());
            var active = await db.Set<Ticket>().SingleAsync(t => t.Id == seed.TicketId);
            Assert.Null(active.EndDate);
            Assert.Equal(fixture.Clock.BusinessDate, active.ValidExitDate);
            Assert.Equal(payment.Value.TicketExceedTransactionId, (await db.Set<TicketExceedTransaction>().SingleAsync()).Id);
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [TicketArchive] DROP CONSTRAINT [TestRejectSoftwareArchive];");
        }

        var completed = await fixture.SendAsync(request, seed.ExitGate, seed.ExitCashier, "ExitCashier");
        completed.ThrowIfFailed();
        AssertSoftwareResponse(completed.Value);
        await fixture.Logs.WaitForAsync(log => log.Contains("Injected SignalR notification failure"));
        var replay = await fixture.SendAsync(request, seed.ExitGate, seed.ExitCashier, "ExitCashier");
        replay.ThrowIfFailed();
        Assert.Equal(completed.Value, replay.Value);
        Assert.Single(fixture.Notifications.Exited);
        Assert.Single(fixture.Notifications.Operations);

        var sanctions = await fixture.SendAsync(new CollectTicketExceedTransactionCommand(), seed.ExitGate, seed.Supervisor, "Supervisor");
        sanctions.ThrowIfFailed();
        Assert.Equal(1, sanctions.Value.TotalTransactionsCollected);
        Assert.Equal(50, sanctions.Value.TotalExceedAmountCollected);
        var baseCollection = await fixture.SendAsync(new CollectEnterTicketsCommand(), seed.EntryGate, seed.Supervisor, "Supervisor");
        baseCollection.ThrowIfFailed();
        await using var verified = fixture.OpenDb();
        var archive = Assert.Single(await verified.Set<TicketArchive>().ToListAsync());
        Assert.Equal((seed.TicketId, 70m, fixture.Clock.BusinessDate, seed.Supervisor),
            (archive.Id, archive.TotalPrice, archive.ValidExitDate, archive.CollectedBy!.Value));
        Assert.Equal(payment.Value.TicketExceedTransactionId, (await verified.Set<TicketExceedTransaction>().SingleAsync()).Id);
        Assert.Equal(2, await verified.Set<TicketCollectionTransactionTicket>().CountAsync(item => item.TicketIdSnapshot == seed.TicketId));

        async Task AssertBlockedAsync(Guid ticket, string code, Guid? gate = null)
        {
            var result = await fixture.SendAsync(new ExitTicketCommand { TicketId = ticket }, gate ?? seed.ExitGate,
                seed.ExitCashier, "ExitCashier");
            Assert.Equal(code, Assert.Single(result.Errors).Code);
        }
    }

    [SqlBrokerFact]
    public async Task MqttQrCompletesSubscriberExitWithoutB1AcrossRestartAndHardwareCanBeRestored()
    {
        var settings = SoftwareExitSettings();
        await using var fixture = await Fixture.StartAsync(EntranceTime(), enableMqttNetworkServices: true,
            entranceSettings: settings);
        var seed = await fixture.SeedAsync(sameDay: true);
        var subscriber = Ticket.Create(fixture.Clock.LocalNow, 0, seed.EntryGate, seed.EntryCashier,
            fixture.Clock.BusinessDate, PriceType.Enter);
        subscriber.SetSubscriber("1000", "SUB 100", "00112233");
        subscriber.ConfirmEntry(fixture.Clock.UtcNow);
        await using (var db = fixture.OpenDb())
        {
            db.Add(subscriber);
            await db.SaveChangesAsync();
        }

        using var simulator = new MqttFactory().CreateMqttClient();
        var commands = Channel.CreateUnbounded<MqttApplicationMessage>();
        await SubscribeToCommandsAsync(simulator, fixture.Port, commands.Writer);
        await fixture.Logs.WaitForAsync(log => log.Contains("MQTT subscriber connected"));
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscription confirmed"));
        var qr = await fixture.GetTicketQrAsync(subscriber.Id);
        Assert.True((await simulator.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("publish/network/54321").WithPayload(Encoding.ASCII.GetBytes(qr))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build())).IsSuccess);

        var capture = await fixture.Notifications.Captures.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(subscriber.Id, capture.TicketId);
        Assert.Equal("Completed", capture.OperationStatus);
        Assert.True(capture.IsValidForExit);
        Assert.True(capture.TicketExited);
        Assert.True(capture.WorkflowSucceeded);
        Assert.False(capture.GateOpened);
        Assert.Empty(capture.OpenGateTopic!);
        Assert.Empty(capture.OpenGatePayload!);
        var software = Assert.Single(fixture.Notifications.Operations);
        AssertSoftwareResponse(software);
        await fixture.RestartAsync();
        using (var budget = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => commands.Reader.ReadAsync(budget.Token).AsTask());
        output.WriteLine("Loopback MQTT monitor observed 0 B1 frames for software QR exit and software-mode restart.");

        settings["GateOperation:EnableExitGateMqtt"] = "true";
        await fixture.RestartAsync();
        using var cashier = await fixture.CreateHttpClientAsync(seed.ExitGate, seed.ExitCashier);
        using var response = await cashier.PostAsJsonAsync("/api/Ticket/ExitTicket", new ExitTicketCommand { TicketId = seed.TicketId });
        var hardwareJson = await response.Content.ReadAsStringAsync();
        output.WriteLine($"Hardware exit HTTP {(int)response.StatusCode}: {hardwareJson}");
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var hardware = Assert.IsType<GateOperationResponse>(await response.Content.ReadFromJsonAsync<GateOperationResponse>());
        Assert.Equal(("Hardware", "Pending", false, false), (hardware.CompletionMode, hardware.Status, hardware.IsAcknowledged, hardware.Archived));
        var frame = await ReadFrameAsync(commands.Reader);
        AssertOpen(frame, hardware, 54321);
        await fixture.WaitForStatusAsync(hardware.OperationId, GateOperationStatus.Published);
        await PublishAckAsync(simulator, "ack/network/54321", MakeAck(frame));
        await fixture.WaitForStatusAsync(hardware.OperationId, GateOperationStatus.Completed);
        using var completed = await cashier.GetAsync($"/api/Gate/operations/{hardware.OperationId}");
        var completedJson = await completed.Content.ReadAsStringAsync();
        output.WriteLine($"Hardware history HTTP {(int)completed.StatusCode}: {completedJson}");
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var confirmed = Assert.IsType<GateOperationResponse>(await completed.Content.ReadFromJsonAsync<GateOperationResponse>());
        Assert.Equal(("Hardware", "Completed", true, true),
            (confirmed.CompletionMode, confirmed.Status, confirmed.IsAcknowledged, confirmed.Archived));

        await using var verified = fixture.OpenDb();
        Assert.Empty(await verified.Set<Ticket>().ToListAsync());
        var archive = await verified.Set<TicketArchive>().SingleAsync(x => x.Id == subscriber.Id);
        Assert.Equal((true, "1000", "SUB 100", "00112233", software.OperationId),
            (archive.IsSubscriber, archive.SubscriberNationalId, archive.VehicleNumber, archive.CardNumber, archive.ExitOperationId!.Value));
        Assert.False(commands.Reader.TryRead(out _));
    }

    [SqlBrokerFact]
    public async Task DisablingExitMqttCancelsUnsentHardwareWorkAndKeepsGenuineHardwareOutcomes()
    {
        var time = EntranceTime();
        var publisher = new GateFrameSpy();
        await using var fixture = await Fixture.StartAsync(time, entranceSettings: SoftwareExitSettings(), publisher: publisher);
        var seed = await fixture.SeedAsync(sameDay: true);
        var second = await fixture.CreateTicketAsync(seed, 20, confirmEntry: true);
        var third = await fixture.CreateTicketAsync(seed, 20, confirmEntry: true);
        var fourth = await fixture.CreateTicketAsync(seed, 20, confirmEntry: true);
        var unsent = LegacyExit(seed.TicketId, 54321);
        var published = LegacyExit(second.Id, 2);
        published.BeginDispatch(fixture.Clock.UtcNow);
        published.MarkPublished(fixture.Clock.UtcNow);
        var acknowledged = LegacyExit(third.Id, 3);
        acknowledged.BeginDispatch(fixture.Clock.UtcNow);
        acknowledged.MarkPublished(fixture.Clock.UtcNow);
        Assert.True(acknowledged.Acknowledge(0, fixture.Clock.UtcNow));
        var claimed = LegacyExit(fourth.Id, 4);
        claimed.BeginDispatch(fixture.Clock.UtcNow);

        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscription confirmed"));
        await using (var db = fixture.OpenDb())
        {
            db.AddRange(unsent, published, acknowledged, claimed);
            await db.SaveChangesAsync();
        }

        var refused = await fixture.SendAsync(new AdvanceGateOperation(unsent.Id, GateTransition.Dispatch),
            seed.ExitGate, seed.ExitCashier, "ExitCashier");
        refused.ThrowIfFailed();
        Assert.Null(refused.Value.DispatchAttemptDeadlineUtc);
        await fixture.WaitForStatusAsync(unsent.Id, GateOperationStatus.Failed);
        await fixture.WaitForStatusAsync(acknowledged.Id, GateOperationStatus.Completed);
        time.Advance(TimeSpan.FromSeconds(3));
        await fixture.WaitForStatusAsync(published.Id, GateOperationStatus.TimedOut);
        await fixture.WaitForStatusAsync(claimed.Id, GateOperationStatus.Unknown);

        var fresh = await fixture.SendAsync(new ExitTicketCommand { TicketId = seed.TicketId, RequestId = Guid.NewGuid() },
            seed.ExitGate, seed.ExitCashier, "ExitCashier");
        fresh.ThrowIfFailed();
        AssertSoftwareResponse(fresh.Value);

        await using var verified = fixture.OpenDb();
        var rows = await verified.Set<GateOperation>().Where(x => x.Id != fresh.Value.OperationId).ToDictionaryAsync(x => x.Id);
        Assert.All(rows.Values, row =>
        {
            Assert.Equal(GateOperationCompletionMode.Hardware, row.CompletionMode);
            Assert.Null(row.ReservedTicketId);
            Assert.Null(row.ReservedControllerId);
        });
        Assert.Equal("GateExitMqttDisabled", rows[unsent.Id].ErrorCode);
        Assert.Null(rows[unsent.Id].DispatchStartedAtUtc);
        Assert.Equal("GateAckTimeout", rows[published.Id].ErrorCode);
        Assert.NotNull(rows[published.Id].PublishedAtUtc);
        Assert.Null(rows[published.Id].AckStatusCode);
        Assert.Equal("GateDeliveryUnknown", rows[claimed.Id].ErrorCode);
        Assert.Equal((byte)0, rows[acknowledged.Id].AckStatusCode);
        var archives = await verified.Set<TicketArchive>().ToDictionaryAsync(x => x.Id);
        Assert.Equal(2, archives.Count);
        Assert.Equal(acknowledged.Id, archives[third.Id].ExitOperationId);
        Assert.Equal(fresh.Value.OperationId, archives[seed.TicketId].ExitOperationId);
        Assert.Equal(2, await verified.Set<Ticket>().CountAsync());
        Assert.Empty(publisher.Frames);

        GateOperation LegacyExit(Guid ticket, ushort controller) => new(Guid.NewGuid(), ticket, seed.ExitGate,
            seed.ExitCashier, controller, GateOperationType.Exit, fixture.Clock.UtcNow);
    }

    private static Dictionary<string, string?> SoftwareExitSettings() => new() { ["GateOperation:EnableExitGateMqtt"] = "false" };

    private sealed class GateFrameSpy : IMqttPublisher
    {
        public System.Collections.Concurrent.ConcurrentQueue<byte[]> Frames { get; } = new();

        public Task EnsureConnectedAsync(CancellationToken ct) => Task.CompletedTask;

        public Task PublishBytesAsync(string topic, ReadOnlyMemory<byte> payload, MqttPublishOptions options,
            CancellationToken ct, Func<bool>? canPublish)
        {
            Frames.Enqueue(payload.ToArray());
            return Task.CompletedTask;
        }

        public Task PublishStringAsync(string topic, string payload, MqttPublishOptions options, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private static void AssertSoftwareResponse(GateOperationResponse operation)
    {
        Assert.Equal(("Exit", "Completed", "Software"), (operation.OperationType, operation.Status, operation.CompletionMode));
        Assert.True(operation.Archived);
        Assert.True(operation.TicketExited);
        Assert.False(operation.IsAcknowledged);
        Assert.False(operation.RetryAllowed);
        Assert.False(operation.RequiresReconciliation);
        Assert.Null(operation.AcknowledgedAtUtc);
        Assert.Null(operation.PublishedAtUtc);
        Assert.Null(operation.AckStatusCode);
        Assert.Null(operation.ReasonCode);
        Assert.Empty(operation.Topic);
        Assert.Empty(operation.Payload);
        Assert.True(long.TryParse(operation.CommandId, out var commandId) && commandId > 0);
    }
}
