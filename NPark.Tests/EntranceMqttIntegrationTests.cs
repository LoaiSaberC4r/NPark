using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using NPark.Application.Feature.LprSignalRManagement.Command.VehicleDetected;
using NPark.Application.Feature.TicketsManagement.Command.Add;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.FileNames;
using NPark.Infrastructure;
using Xunit;

namespace NPark.Tests;

public sealed partial class ParkingWorkflowIntegrationTests
{
    private const ushort EntranceController = 54320;
    private const string EntranceCamera = "127.0.0.1";
    private const string ExitCamera = "127.0.0.22";

    [SqlBrokerFact]
    public async Task SubscriberLockUsesOneTicketlessOperationAndKeepsTheExistingEntryFlow()
    {
        var time = EntranceTime();
        await using var fixture = await Fixture.StartAsync(time, entranceSettings: EntranceSettings());
        var seed = await fixture.SeedAsync(sameDay: true);
        await ConfigureEntranceAsync(fixture, seed);
        await using (var db = fixture.OpenDb())
        {
            var price = PricingScheme.Create("Test membership", DurationType.Days, null, null, false, 80, 30, null);
            db.Add(price);
            await db.SaveChangesAsync();
            db.AddRange(
                ParkingMemberships.Create("Valid test member", "100", "100", "SUB 101", "11223344", price.Id,
                    fixture.Clock.LocalNow.AddDays(-1), fixture.Clock.LocalNow.AddDays(1)),
                ParkingMemberships.Create("Expired test member", "200", "200", "OLD 101", "88776655", price.Id,
                    fixture.Clock.LocalNow.AddDays(-2), fixture.Clock.LocalNow));
            var exit = await db.Set<ParkingGate>().SingleAsync(x => x.Id == seed.ExitGate);
            db.Entry(exit).Property(x => x.LprIp).CurrentValue = ExitCamera;
            exit.SetControlIp("legacy-address");
            await db.SaveChangesAsync();
        }

        using var simulator = new MqttFactory().CreateMqttClient();
        var commands = Channel.CreateUnbounded<MqttApplicationMessage>();
        await SubscribeToCommandsAsync(simulator, fixture.Port, commands.Writer);
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscription confirmed"));
        using var cashier = await fixture.CreateHttpClientAsync(seed.EntryGate, seed.EntryCashier);
        using var forbidden = await cashier.PostAsJsonAsync("/api/Gate/open",
            new { controllerId = EntranceController.ToString(), operationType = "LockCardButton" });
        Assert.False(forbidden.IsSuccessStatusCode);

        await DetectAsync(cashier, EntranceCamera, "VISITOR");
        await DetectAsync(cashier, EntranceCamera, "OLD 101");
        await DetectAsync(cashier, ExitCamera, "SUB 101");
        Assert.Equal(3, fixture.Notifications.Lpr.Count);
        Assert.False(commands.Reader.TryRead(out _));

        var detection = DetectAsync(cashier, EntranceCamera, " SUB 101 ");
        var duplicate = DetectAsync(cashier, EntranceCamera, "SUB101");
        var lockFrame = await ReadFrameAsync(commands.Reader);
        Assert.Equal($"sub/network/{EntranceController}", lockFrame.Topic);
        var lockCommand = BinaryPrimitives.ReadUInt64BigEndian(lockFrame.PayloadSegment.AsSpan()[3..11]);
        Assert.Equal(GatePacketCodec.EncodeLock(EntranceController, lockCommand), lockFrame.PayloadSegment.ToArray());
        Assert.Equal(3, fixture.Notifications.Lpr.Count);
        await WaitForAsync(fixture, db => db.LprCaptures.CountAsync(), count => count == 5);
        await using (var db = fixture.OpenDb())
        {
            var capture = await db.LprCaptures.SingleAsync(x => x.GateId == seed.ExitGate);
            Assert.Null(capture.ControllerId);
            var gateOperation = await db.Set<GateOperation>().SingleAsync();
            Assert.Equal(GateOperationType.LockCardButton, gateOperation.Type);
            Assert.Null(gateOperation.TicketIdSnapshot);
            Assert.Equal(1, await db.Set<Ticket>().CountAsync());
        }
        var lockOperation = await WaitForAsync(fixture, db => db.Set<GateOperation>().FirstOrDefaultAsync(),
            operation => operation?.Status == GateOperationStatus.Published);
        await PublishAckAsync(simulator, $"ack/network/{EntranceController}", MakeAck(lockFrame));
        await Task.WhenAll(detection, duplicate);
        Assert.Equal(5, fixture.Notifications.Lpr.Count);
        await fixture.WaitForStatusAsync(lockOperation!.Id, GateOperationStatus.Completed);
        Assert.False(commands.Reader.TryRead(out _));

        var ticket = await fixture.SendAsync(new AddTicketCommand { IsSubscriber = true, VehicleNumber = "SUB101", CardNumber = "11223344" },
            seed.EntryGate, seed.EntryCashier, "EntranceCashier");
        ticket.ThrowIfFailed();
        using var open = await cashier.PostAsJsonAsync("/api/Gate/open", new
        {
            controllerId = EntranceController.ToString(), operationType = "Entry", ticketId = ticket.Value.TicketId,
            requestId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, open.StatusCode);
        var authorized = Assert.IsType<GateOperationResponse>(await open.Content.ReadFromJsonAsync<GateOperationResponse>());
        var openFrame = await ReadFrameAsync(commands.Reader);
        AssertOpen(openFrame, authorized, EntranceController);
        Assert.NotEqual(lockCommand.ToString(), authorized.CommandId);
        await fixture.WaitForStatusAsync(authorized.OperationId, GateOperationStatus.Published);
        var lateLockAck = MakeAck(lockFrame);
        lateLockAck[11] = 1;
        await PublishAckAsync(simulator, $"ack/network/{EntranceController}", lateLockAck);
        await fixture.Logs.WaitForAsync(log => log.Contains("LateAckForTerminalOperation"));
        await using (var db = fixture.OpenDb())
        {
            Assert.Equal(EntryConfirmationStatus.Pending,
                (await db.Set<Ticket>().FindAsync(ticket.Value.TicketId))!.EntryConfirmationStatus);
        }
        await PublishAckAsync(simulator, $"ack/network/{EntranceController}", MakeAck(openFrame));
        await fixture.WaitForStatusAsync(authorized.OperationId, GateOperationStatus.Completed);
        time.Advance(TimeSpan.FromSeconds(61));
        await DetectAsync(cashier, EntranceCamera, "SUB101");
        await using var verify = fixture.OpenDb();
        var issued = await verify.Set<Ticket>().SingleAsync(x => x.Id == ticket.Value.TicketId);
        Assert.Equal((true, "11223344", 0m, EntryConfirmationStatus.Confirmed),
            (issued.IsSubscriber, issued.CardNumber, issued.Price, issued.EntryConfirmationStatus));
        Assert.Equal(2, await verify.Set<GateOperation>().CountAsync());
        Assert.All(await verify.Set<GateOperation>().ToListAsync(), op => Assert.Null(op.ReservedControllerId));
    }

    [SqlBrokerFact]
    public async Task SubscriberLockUsesRegisteredLprIpForDirectAndRelayedRequests()
    {
        // The API listens on loopback, so its HTTP peer differs from this registered camera.
        const string registeredCamera = "192.0.2.20";
        const string detectionPath = "/api/lpr-signalr/vehicle-detected";
        var time = EntranceTime();
        var settings = EntranceSettings();
        settings["SubscriberButtonLock:SupportedControllerIds:1"] = "54321";
        await using var fixture = await Fixture.StartAsync(time, entranceSettings: settings);
        var seed = await fixture.SeedAsync(sameDay: true);
        await using (var db = fixture.OpenDb())
        {
            var entrance = await db.Set<ParkingGate>().SingleAsync(x => x.Id == seed.EntryGate);
            db.Entry(entrance).Property(x => x.LprIp).CurrentValue = registeredCamera;
            entrance.SetControlIp("54310");
            var exit = await db.Set<ParkingGate>().SingleAsync(x => x.Id == seed.ExitGate);
            db.Entry(exit).Property(x => x.LprIp).CurrentValue = ExitCamera;
            var price = PricingScheme.Create("Test membership", DurationType.Days, null, null, false, 80, 30, null);
            db.Add(price);
            await db.SaveChangesAsync();
            db.Add(ParkingMemberships.Create("Valid test member", "100", "100", "SUB 101", "11223344", price.Id,
                fixture.Clock.LocalNow.AddDays(-1), fixture.Clock.LocalNow.AddDays(1)));
            await db.SaveChangesAsync();
        }

        using var simulator = new MqttFactory().CreateMqttClient();
        var commands = Channel.CreateUnbounded<MqttApplicationMessage>();
        await SubscribeToCommandsAsync(simulator, fixture.Port, commands.Writer);
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscription confirmed"));
        using var cashier = await fixture.CreateHttpClientAsync(seed.EntryGate, seed.EntryCashier);
        using var unknown = await cashier.PostAsJsonAsync(detectionPath,
            new LprVehicleDetectedCommand { LprIp = "192.0.2.30", VehicleNumber = "SUB 101" });
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Contains("LPR_IP_NOT_FOUND", await unknown.Content.ReadAsStringAsync());

        await DetectAsync(cashier, registeredCamera, "SUB 101");
        await DetectAsync(cashier, ExitCamera, "SUB 101");
        Assert.Equal(new[] { seed.EntryGate, seed.ExitGate }, fixture.Notifications.Lpr.Select(x => x.GateId).ToArray());
        await using (var db = fixture.OpenDb())
        {
            Assert.Equal(2, await db.LprCaptures.CountAsync());
            Assert.Empty(await db.Set<GateOperation>().ToListAsync());
            var entrance = await db.Set<ParkingGate>().SingleAsync(x => x.Id == seed.EntryGate);
            entrance.SetControlIp(EntranceController.ToString());
            await db.SaveChangesAsync();
        }

        var relayBuilder = WebApplication.CreateSlimBuilder();
        relayBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var relay = relayBuilder.Build();
        relay.MapPost(detectionPath, async (LprVehicleDetectedCommand detection, CancellationToken ct) =>
        {
            using var response = await cashier.PostAsJsonAsync(detectionPath, detection, ct);
            return Results.StatusCode((int)response.StatusCode);
        });
        await relay.StartAsync();
        using var relayClient = new HttpClient { BaseAddress = new Uri(Assert.Single(relay.Urls)) };

        foreach (var sender in new[] { cashier, relayClient })
        {
            var detected = DetectAsync(sender, registeredCamera, "SUB 101");
            var command = await ReadFrameAsync(commands.Reader);
            Assert.Equal($"sub/network/{EntranceController}", command.Topic);
            var commandId = BinaryPrimitives.ReadUInt64BigEndian(command.PayloadSegment.AsSpan()[3..11]);
            Assert.Equal(GatePacketCodec.EncodeLock(EntranceController, commandId), command.PayloadSegment.ToArray());
            var operation = await WaitForAsync(fixture,
                db => db.Set<GateOperation>().SingleOrDefaultAsync(x => x.CommandId == (long)commandId),
                x => x?.Status == GateOperationStatus.Published);
            await PublishAckAsync(simulator, $"ack/network/{EntranceController}", MakeAck(command));
            await detected;
            await fixture.WaitForStatusAsync(operation!.Id, GateOperationStatus.Completed);
            time.Advance(TimeSpan.FromSeconds(61));
        }

        settings["SubscriberButtonLock:Enabled"] = "false";
        await fixture.RestartAsync();
        using var disabledCashier = await fixture.CreateHttpClientAsync(seed.EntryGate, seed.EntryCashier);
        await DetectAsync(disabledCashier, registeredCamera, "SUB 101");
        Assert.False(commands.Reader.TryRead(out _));
        Assert.Equal(5, fixture.Notifications.Lpr.Count);
        await using var verify = fixture.OpenDb();
        Assert.Equal(5, await verify.LprCaptures.CountAsync());
        var operations = await verify.Set<GateOperation>().ToListAsync();
        Assert.Equal(2, operations.Count);
        Assert.All(operations, op =>
        {
            Assert.Equal(GateOperationType.LockCardButton, op.Type);
            Assert.Equal(seed.EntryGate, op.GateId);
            Assert.Null(op.TicketIdSnapshot);
            Assert.Null(op.ReservedControllerId);
        });
    }

    [SqlBrokerFact]
    public async Task SubscriberLockFailureAndTimeoutReleaseControllerAndStillNotify()
    {
        var time = EntranceTime();
        await using var fixture = await Fixture.StartAsync(time, entranceSettings: EntranceSettings());
        var seed = await fixture.SeedAsync(sameDay: true);
        await ConfigureEntranceAsync(fixture, seed);
        await using (var db = fixture.OpenDb())
        {
            var price = PricingScheme.Create("Test membership", DurationType.Days, null, null, false, 80, 30, null);
            db.Add(price);
            await db.SaveChangesAsync();
            db.Add(ParkingMemberships.Create("Valid test member", "100", "100", "SUB 101", "11223344", price.Id,
                fixture.Clock.LocalNow.AddDays(-1), fixture.Clock.LocalNow.AddDays(1)));
            await db.SaveChangesAsync();
        }
        using var simulator = new MqttFactory().CreateMqttClient();
        var commands = Channel.CreateUnbounded<MqttApplicationMessage>();
        await SubscribeToCommandsAsync(simulator, fixture.Port, commands.Writer);
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscription confirmed"));
        using var cashier = await fixture.CreateHttpClientAsync(seed.EntryGate, seed.EntryCashier);

        foreach (var failure in new[] { GateOperationStatus.Failed, GateOperationStatus.TimedOut })
        {
            var detection = DetectAsync(cashier, EntranceCamera, "SUB 101");
            var frame = await ReadFrameAsync(commands.Reader);
            var commandId = (long)BinaryPrimitives.ReadUInt64BigEndian(frame.PayloadSegment.AsSpan()[3..11]);
            var original = await WaitForAsync(fixture,
                db => db.Set<GateOperation>().SingleOrDefaultAsync(x => x.CommandId == commandId),
                x => x?.Status == GateOperationStatus.Published);
            if (failure == GateOperationStatus.Failed)
            {
                var failedAck = MakeAck(frame);
                failedAck[11] = 1;
                await PublishAckAsync(simulator, $"ack/network/{EntranceController}", failedAck);
            }
            else
            {
                time.Advance(TimeSpan.FromSeconds(2));
            }
            await detection;
            await fixture.WaitForStatusAsync(original!.Id, failure);
            time.Advance(TimeSpan.FromSeconds(61));
        }
        await using var verify = fixture.OpenDb();
        Assert.Equal(2, fixture.Notifications.Lpr.Count);
        Assert.Equal(2, await verify.Set<GateOperation>().CountAsync());
        Assert.All(await verify.Set<GateOperation>().ToListAsync(), op => Assert.Null(op.ReservedControllerId));
        Assert.Equal(seed.TicketId, (await verify.Set<Ticket>().SingleAsync()).Id);
        Assert.False(commands.Reader.TryRead(out _));
    }

    [SqlBrokerFact]
    public async Task VisitorReplayMatchesLatestCaptureThenIssuesCardOnlyAndPreservesBothAttempts()
    {
        var time = EntranceTime();
        var systemUserId = Guid.NewGuid();
        await using var fixture = await Fixture.StartAsync(time, entranceSettings: EntranceSettings(systemUserId));
        var seed = await fixture.SeedAsync(sameDay: true);
        await ConfigureEntranceAsync(fixture, seed, systemUserId, PriceType.Exit);
        using var simulator = new MqttFactory().CreateMqttClient();
        var (commands, storageAcks) = await SubscribeToEntranceAsync(fixture, simulator);
        using var cashier = await fixture.CreateHttpClientAsync(seed.EntryGate, seed.EntryCashier);
        await DetectAsync(cashier, EntranceCamera, "OLDER");
        time.Advance(TimeSpan.FromSeconds(1));
        await DetectAsync(cashier, EntranceCamera, "LATEST");
        await using (var db = fixture.OpenDb())
        {
            db.LprCaptures.AddRange(
                new LprCapture(seed.EntryGate, EntranceController, EntranceCamera, "FUTURE", fixture.Clock.UtcNow.AddMinutes(10)),
                new LprCapture(seed.EntryGate, 7, EntranceCamera, "OTHER CONTROLLER", fixture.Clock.UtcNow));
            await db.SaveChangesAsync();
        }
        var receipt = fixture.Clock.UtcNow;
        var packet = VisitorPacket(1, "007B7DAB");
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => PublishVisitorAsync(simulator, packet)));
        for (var i = 0; i < 4; i++)
            AssertStorageAck(await ReadFrameAsync(storageAcks), packet);

        var command = await ReadFrameAsync(commands);
        var hardware = await WaitForAsync(fixture, db => db.ParkingHardwareEvents.FirstOrDefaultAsync(),
            row => row?.GateOperationId.HasValue == true);
        var operationId = hardware!.GateOperationId!.Value;
        var ticketId = hardware.TicketId!.Value;
        await fixture.WaitForStatusAsync(operationId, GateOperationStatus.Published);
        await using (var db = fixture.OpenDb())
        {
            var gateOperation = await db.Set<GateOperation>().SingleAsync();
            Assert.Equal((hardware.Id, ticketId, EntranceController, true),
                (gateOperation.Id, gateOperation.TicketIdSnapshot!.Value, (ushort)gateOperation.ControllerId,
                    gateOperation.IsAutomaticVisitorEntry));
            Assert.Equal(GatePacketCodec.EncodeOpen(EntranceController, (ulong)gateOperation.CommandId), command.PayloadSegment.ToArray());
            var ticket = await db.Set<Ticket>().SingleAsync(x => x.Id == ticketId);
            Assert.Equal((false, "007B7DAB", "LATEST", systemUserId, 0m, BasePaymentStatus.Unpaid, EntryConfirmationStatus.Pending),
                (ticket.IsSubscriber, ticket.CardNumber, ticket.VehicleNumber, ticket.UserId, ticket.Price, ticket.BasePaymentStatus,
                    ticket.EntryConfirmationStatus));
            Assert.False(ticket.IsCashierCollected);
            Assert.Equal(receipt, hardware.ReceivedAtUtc);
            Assert.Equal(fixture.Clock.ToBusinessTime(receipt), ticket.StartDate);
            Assert.Equal(DateOnly.FromDateTime(ticket.StartDate), ticket.ValidExitDate);
            Assert.Equal(0x1D, hardware.Flags);
            Assert.Equal(fixture.Clock.ToUtc(DateTimeOffset.FromUnixTimeSeconds(0x6B0527E0).DateTime), hardware.HardwareOccurredAtUtc);
            Assert.Equal(packet, hardware.RawPayload);
            Assert.Equal(ticketId, (await db.LprCaptures.FindAsync(hardware.LprCaptureId))!.TicketId);
            Assert.True(Assert.Single(fixture.Notifications.Added).IsAutomatic);
        }
        var conflict = VisitorPacket(1, "DEADBEEF");
        await PublishVisitorAsync(simulator, conflict);
        await fixture.Logs.WaitForAsync(log => log.Contains("ParkingEventPayloadConflict"));
        Assert.False(storageAcks.TryRead(out _));
        await PublishAckAsync(simulator, $"ack/network/{EntranceController}", MakeAck(command));
        await WaitForAsync(fixture, db => db.ParkingHardwareEvents.FirstOrDefaultAsync(),
            row => row?.ProcessingStatus == ParkingEventProcessingStatus.Completed);

        time.Advance(TimeSpan.FromSeconds(2));
        var secondPacket = VisitorPacket(2, "AABBCCDD");
        await PublishVisitorAsync(simulator, secondPacket);
        AssertStorageAck(await ReadFrameAsync(storageAcks), secondPacket);
        var secondCommand = await ReadFrameAsync(commands);
        await using (var db = fixture.OpenDb())
        {
            var cardOnly = await db.Set<Ticket>().SingleAsync(x => x.CardNumber == "AABBCCDD");
            Assert.Null(cardOnly.VehicleNumber);
            Assert.Equal(EntryConfirmationStatus.Pending, cardOnly.EntryConfirmationStatus);
        }
        var secondOperation = await WaitForAsync(fixture,
            db => db.Set<GateOperation>().SingleOrDefaultAsync(x => x.Id != operationId),
            op => op?.Status == GateOperationStatus.Published);
        time.Advance(TimeSpan.FromSeconds(2));
        await fixture.WaitForStatusAsync(secondOperation!.Id, GateOperationStatus.TimedOut);
        await PublishAckAsync(simulator, $"ack/network/{EntranceController}", MakeAck(secondCommand));
        await fixture.RestartAsync();
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscription confirmed"));
        await WaitForAsync(fixture, db => db.ParkingHardwareEvents.CountAsync(x => x.ProcessingStatus == ParkingEventProcessingStatus.Failed),
            count => count == 1);
        await using var verify = fixture.OpenDb();
        Assert.Equal(2, await verify.ParkingHardwareEvents.CountAsync());
        Assert.Equal(2, await verify.Set<GateOperation>().CountAsync());
        Assert.Equal(3, await verify.Set<Ticket>().CountAsync());
        Assert.Null((await verify.Set<GateOperation>().FindAsync(secondOperation.Id))!.ReservedControllerId);
        Assert.Equal(EntryConfirmationStatus.Pending,
            (await verify.Set<Ticket>().SingleAsync(x => x.CardNumber == "AABBCCDD")).EntryConfirmationStatus);
        Assert.False(commands.TryRead(out _));
    }

    [SqlBrokerFact]
    public async Task EntrancePricingAndInvalidHardwareEventsNeverIssueTickets()
    {
        var systemUserId = Guid.NewGuid();
        await using var fixture = await Fixture.StartAsync(EntranceTime(), entranceSettings: EntranceSettings(systemUserId));
        var seed = await fixture.SeedAsync(sameDay: true);
        await ConfigureEntranceAsync(fixture, seed, systemUserId, PriceType.Enter, hasLpr: false);
        using var simulator = new MqttFactory().CreateMqttClient();
        var (commands, storageAcks) = await SubscribeToEntranceAsync(fixture, simulator);
        byte[][] frames =
        [
            VisitorPacket(1, "10203040"),
            VisitorPacket(2, null),
            VisitorPacket(3, "11223344", lane: 2),
            VisitorPacket(4, null, outcome: 4),
            VisitorPacket(5, "AABBCCDD", schema: 2)
        ];
        foreach (var frame in frames)
        {
            await PublishVisitorAsync(simulator, frame);
            AssertStorageAck(await ReadFrameAsync(storageAcks), frame);
        }
        await WaitForAsync(fixture, db => db.ParkingHardwareEvents.CountAsync(x => x.ProcessedAtUtc != null),
            count => count == frames.Length);
        await using (var db = fixture.OpenDb())
        {
            var configuration = await db.Set<ParkingSystemConfiguration>().SingleAsync();
            db.Entry(configuration).Property(x => x.PriceType).CurrentValue = PriceType.Exit;
            var gate = await db.Set<ParkingGate>().SingleAsync(x => x.Id == seed.EntryGate);
            gate.SetScanner(true);
            db.Remove(await db.Set<User>().SingleAsync(x => x.Id == systemUserId));
            await db.SaveChangesAsync();
        }
        for (uint sequence = 6; sequence <= 8; sequence++)
        {
            var frame = VisitorPacket(sequence, "0099AABB");
            await PublishVisitorAsync(simulator, frame);
            AssertStorageAck(await ReadFrameAsync(storageAcks), frame);
            await WaitForAsync(fixture, db => db.ParkingHardwareEvents.CountAsync(x => x.ProcessedAtUtc != null),
                count => count == sequence);
            await using var db = fixture.OpenDb();
            var gate = await db.Set<ParkingGate>().SingleAsync(x => x.Id == seed.EntryGate);
            gate.SetScanner(false);
            if (sequence == 7)
                gate.SetControlIp("54310");
            await db.SaveChangesAsync();
        }
        await using var verify = fixture.OpenDb();
        Assert.Equal(5, await verify.ParkingHardwareEvents.CountAsync(x => x.ProcessingStatus == ParkingEventProcessingStatus.Rejected));
        Assert.Equal(3, await verify.ParkingHardwareEvents.CountAsync(x => x.ProcessingStatus == ParkingEventProcessingStatus.Ignored));
        var reasons = await verify.ParkingHardwareEvents.Select(x => x.FailureReasonCode).ToListAsync();
        Assert.Contains("AutomaticVisitorEntryRequiresExitPricing", reasons);
        Assert.Contains("ScannerPhotoRequired", reasons);
        Assert.Contains("AutomaticVisitorSystemActorUnavailable", reasons);
        Assert.Contains("EntranceControllerNotMapped", reasons);
        Assert.Empty(await verify.Set<GateOperation>().ToListAsync());
        Assert.Empty(await verify.Set<TicketCollectionTransaction>().ToListAsync());
        Assert.Equal(seed.TicketId, (await verify.Set<Ticket>().SingleAsync()).Id);
        Assert.False(commands.TryRead(out _));
    }

    [SqlBrokerFact]
    public async Task AutomaticEntryRecoversCommittedEventAndTicketWithoutRepublishingAnAuthorizedCommand()
    {
        var time = EntranceTime();
        var actor = Guid.NewGuid();
        await using var fixture = await Fixture.StartAsync(time, entranceSettings: EntranceSettings(actor));
        var seed = await fixture.SeedAsync(sameDay: true);
        await ConfigureEntranceAsync(fixture, seed, actor, PriceType.Exit, hasLpr: false);
        using var simulator = new MqttFactory().CreateMqttClient();
        var (commands, storageAcks) = await SubscribeToEntranceAsync(fixture, simulator);
        await using (var db = fixture.OpenDb())
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [ParkingHardwareEvents] ADD CONSTRAINT " +
                "[TestFailHardwareStorage] CHECK ([ControllerId] = 1)");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [GateOperations] ADD CONSTRAINT " +
                "[TestFailAutomaticAuthorization] CHECK ([IsAutomaticVisitorEntry] = 0)");
        }
        var packet = VisitorPacket(15, "DEAD007D");
        await PublishVisitorAsync(simulator, packet);
        await fixture.Logs.WaitForAsync(log => log.Contains("Parking event intake or storage ACK failed"));
        Assert.False(storageAcks.TryRead(out _));
        await using (var db = fixture.OpenDb())
        {
            Assert.Empty(await db.ParkingHardwareEvents.ToListAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [ParkingHardwareEvents] DROP CONSTRAINT [TestFailHardwareStorage]");
        }
        var receipt = fixture.Clock.UtcNow;
        await PublishVisitorAsync(simulator, packet);
        AssertStorageAck(await ReadFrameAsync(storageAcks), packet);
        await fixture.Logs.WaitForAsync(log => log.Contains("Parking event processing will retry"));
        var hardware = await WaitForAsync(fixture, db => db.ParkingHardwareEvents.FirstOrDefaultAsync(),
            x => x?.ProcessingStatus == ParkingEventProcessingStatus.TicketCreated);
        Assert.NotNull(hardware?.TicketId);
        Assert.Null(hardware.GateOperationId);
        await using (var db = fixture.OpenDb())
        {
            Assert.Empty(await db.Set<GateOperation>().ToListAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [GateOperations] DROP CONSTRAINT [TestFailAutomaticAuthorization]");
        }
        time.Advance(TimeSpan.FromSeconds(10));
        await fixture.RestartAsync();
        var command = await ReadFrameAsync(commands);
        await fixture.WaitForStatusAsync(hardware.Id, GateOperationStatus.Published);
        await fixture.RestartAsync();
        await PublishVisitorAsync(simulator, packet);
        AssertStorageAck(await ReadFrameAsync(storageAcks), packet);
        await PublishAckAsync(simulator, $"ack/network/{EntranceController}", MakeAck(command));
        var complete = await WaitForAsync(fixture, db => db.ParkingHardwareEvents.FirstOrDefaultAsync(),
            x => x?.ProcessingStatus == ParkingEventProcessingStatus.Completed);
        await using var verify = fixture.OpenDb();
        Assert.Equal((hardware.Id, hardware.TicketId, receipt), (complete!.Id, complete.TicketId, complete.ReceivedAtUtc));
        var ticket = await verify.Set<Ticket>().SingleAsync(x => x.Id == complete.TicketId);
        Assert.Equal((fixture.Clock.ToBusinessTime(receipt), EntryConfirmationStatus.Confirmed, "DEAD007D"),
            (ticket.StartDate, ticket.EntryConfirmationStatus, ticket.CardNumber));
        Assert.Null(ticket.VehicleNumber);
        Assert.Single(await verify.Set<GateOperation>().ToListAsync());
        Assert.False(commands.TryRead(out _));
    }

    [SqlFact]
    public async Task LprArchiveRollsBackOnFailureAndPreservesTheOriginalCaptureIdAndTicketLink()
    {
        var time = EntranceTime();
        var settings = new Dictionary<string, string?> { ["LprCapture:ArchiveBatchSize"] = "1" };
        await using var fixture = await Fixture.StartAsync(time, entranceSettings: settings);
        var seed = await fixture.SeedAsync(sameDay: true);
        var now = fixture.Clock.UtcNow;
        var old = new LprCapture(seed.EntryGate, EntranceController, EntranceCamera, "ARCHIVED", now.AddDays(-3).AddSeconds(-1));
        var fresh = new LprCapture(seed.EntryGate, EntranceController, EntranceCamera, "RETAINED", now.AddDays(-3));
        old.Claim(seed.TicketId, now.AddDays(-3));
        await using (var db = fixture.OpenDb())
        {
            var packet = VisitorPacket(11, "00000011");
            var hardware = new ParkingHardwareEvent(EntranceController, packet[3..11], 1, 1, 1, 0, "00000011", 0,
                null, now.AddDays(-3), packet);
            hardware.RecordTicket(seed.TicketId, old.Id);
            db.AddRange(old, fresh, hardware);
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [LprCapturesArchive] ADD CONSTRAINT " +
                "[TestFailArchive] CHECK ([ControllerId] IS NULL)");
        }
        await fixture.RestartAsync();
        await fixture.Logs.WaitForAsync(log => log.Contains("LPR archive batch failed"));
        await using (var db = fixture.OpenDb())
        {
            Assert.Equal(2, await db.LprCaptures.CountAsync());
            Assert.Empty(await db.LprCapturesArchive.ToListAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [LprCapturesArchive] DROP CONSTRAINT [TestFailArchive]");
        }
        await fixture.RestartAsync();
        await WaitForAsync(fixture, db => db.LprCapturesArchive.CountAsync(), count => count == 1);
        await fixture.RestartAsync();
        await using var verify = fixture.OpenDb();
        var archived = await verify.LprCapturesArchive.SingleAsync();
        Assert.Equal((old.Id, old.CapturedAtUtc, old.ProcessedAtUtc, old.TicketId, now),
            (archived.Id, archived.CapturedAtUtc, archived.ProcessedAtUtc, archived.TicketId, archived.ArchivedAtUtc));
        Assert.Equal(old.Id, (await verify.ParkingHardwareEvents.SingleAsync()).LprCaptureId);
        Assert.Equal(fresh.Id, (await verify.LprCaptures.SingleAsync()).Id);
        Assert.Equal(seed.TicketId, (await verify.Set<Ticket>().SingleAsync()).Id);
    }

    private static ManualParkingTime EntranceTime() => new(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));

    private static Dictionary<string, string?> EntranceSettings(Guid? systemUserId = null) => new()
    {
        ["GateOperation:EnableExitGateMqtt"] = "false",
        ["SubscriberButtonLock:Enabled"] = "true",
        ["SubscriberButtonLock:SupportedControllerIds:0"] = EntranceController.ToString(),
        ["MqttParkingEvents:Enabled"] = systemUserId.HasValue.ToString(),
        ["AutomaticVisitorEntry:Enabled"] = systemUserId.HasValue.ToString(),
        ["AutomaticVisitorEntry:SystemUserId"] = systemUserId?.ToString(),
        ["AutomaticVisitorEntry:SupportedControllerIds:0"] = EntranceController.ToString()
    };

    private static async Task ConfigureEntranceAsync(Fixture fixture, Seed seed, Guid? systemUserId = null,
        PriceType pricing = PriceType.Enter, bool hasLpr = true)
    {
        await using var db = fixture.OpenDb();
        var gate = await db.Set<ParkingGate>().SingleAsync(x => x.Id == seed.EntryGate);
        if (hasLpr)
            db.Entry(gate).Property(x => x.LprIp).CurrentValue = EntranceCamera;
        var configuration = await db.Set<ParkingSystemConfiguration>().SingleAsync();
        db.Entry(configuration).Property(x => x.PriceType).CurrentValue = pricing;
        if (systemUserId is Guid id)
        {
            var systemUser = Fixture.NewUser("test-mqtt-actor", SystemRoles.EntranceCashierRoleId);
            db.Entry(systemUser).Property(x => x.Id).CurrentValue = id;
            db.Add(systemUser);
        }
        await db.SaveChangesAsync();
    }

    private static async Task DetectAsync(HttpClient cashier, string camera, string vehicle)
    {
        using var response = await cashier.PostAsJsonAsync("/api/lpr-signalr/vehicle-detected",
            new LprVehicleDetectedCommand { LprIp = camera, VehicleNumber = vehicle });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<(ChannelReader<MqttApplicationMessage> Commands, ChannelReader<MqttApplicationMessage> StorageAcks)>
        SubscribeToEntranceAsync(Fixture fixture, IMqttClient simulator)
    {
        var commands = Channel.CreateUnbounded<MqttApplicationMessage>();
        var acks = Channel.CreateUnbounded<MqttApplicationMessage>();
        await SubscribeToCommandsAsync(simulator, fixture.Port, commands.Writer);
        simulator.ApplicationMessageReceivedAsync += args =>
        {
            if (args.ApplicationMessage.Topic == $"parking/v1/ack/{EntranceController}")
                acks.Writer.TryWrite(args.ApplicationMessage);
            return Task.CompletedTask;
        };
        await simulator.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter($"parking/v1/ack/{EntranceController}", MqttQualityOfServiceLevel.AtLeastOnce).Build());
        await fixture.Logs.WaitForAsync(log => log.Contains("MQTT parking event subscription confirmed"));
        await fixture.Logs.WaitForAsync(log => log.Contains("Gate ACK subscription confirmed"));
        return (commands.Reader, acks.Reader);
    }

    private static byte[] VisitorPacket(uint sequence, string? uid, byte lane = 1, byte outcome = 1, byte schema = 1)
    {
        var card = uid is null ? Array.Empty<byte>() : Convert.FromHexString(uid);
        var packet = new byte[(schema == 1 ? 21 : 27) + card.Length];
        packet[0] = 0x7B;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(1), EntranceController);
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(3), 789);
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(7), sequence);
        packet[11] = schema;
        packet[12] = lane;
        packet[13] = outcome;
        packet[14] = 0x1D;
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(15), 0x6B0527E0);
        packet[19] = (byte)card.Length;
        card.CopyTo(packet, 20);
        packet[^1] = 0x7D;
        return packet;
    }

    private static async Task PublishVisitorAsync(IMqttClient simulator, byte[] packet)
    {
        var result = await simulator.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"parking/v1/event/{EntranceController}").WithPayload(packet)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce).Build());
        Assert.True(result.IsSuccess);
    }

    private static void AssertStorageAck(MqttApplicationMessage ack, byte[] packet)
    {
        Assert.Equal($"parking/v1/ack/{EntranceController}", ack.Topic);
        Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, ack.QualityOfServiceLevel);
        Assert.False(ack.Retain);
        Assert.Equal(ParkingEventPacketCodec.EncodeStorageAck(packet), ack.PayloadSegment.ToArray());
    }

    private static async Task<T> WaitForAsync<T>(Fixture fixture, Func<NParkDBContext, Task<T>> query, Func<T, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            await using var db = fixture.OpenDb();
            var result = await query(db);
            if (predicate(result))
                return result;
            if (timeout.IsCancellationRequested)
                throw new TimeoutException($"The database did not reach the expected state. Last observed value: {result}.");
            await Task.Delay(100);
        }
    }
}
