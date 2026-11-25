using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using NPark.Application.Abstraction;
using NPark.Application.Feature.GateManagement.Command.OpenGate;
using NPark.Application.Feature.TicketsManagement.Command.ExitTicket;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using Xunit;

namespace NPark.Tests;

public sealed partial class ParkingWorkflowIntegrationTests
{
    [SqlBrokerFact]
    public async Task RepeatedEntryAndManualFailuresReleaseReservationsAndUseTheCurrentGateMapping()
    {
        var time = new ManualParkingTime(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
        await using var fixture = await Fixture.StartAsync(time);
        var seed = await fixture.SeedAsync(sameDay: true, exitController: "1");
        var ticket = await fixture.CreateTicketAsync(seed, 20, confirmEntry: false);
        var qr = await fixture.GetTicketQrAsync(ticket.Id);
        using var simulator = new MqttFactory().CreateMqttClient();
        var frames = Channel.CreateUnbounded<MqttApplicationMessage>();
        await SubscribeToCommandsAsync(simulator, fixture.Port, frames.Writer);
        using var cashier = await fixture.CreateHttpClientAsync(seed.EntryGate, seed.EntryCashier);

        async Task<(GateOperationResponse Operation, MqttApplicationMessage Frame)> OpenEntryAsync(ushort controller)
        {
            using var response = await cashier.PostAsJsonAsync("/api/Gate/open", new OpenGateCommand
            {
                OperationType = "Entry", ControllerId = controller.ToString(), TicketId = ticket.Id, RequestId = Guid.NewGuid()
            });
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            var operation = Assert.IsType<GateOperationResponse>(await response.Content.ReadFromJsonAsync<GateOperationResponse>());
            var frame = await ReadFrameAsync(frames.Reader);
            AssertOpen(frame, operation, controller);
            await fixture.WaitForStatusAsync(operation.OperationId, GateOperationStatus.Published);
            return (operation, frame);
        }

        var first = await OpenEntryAsync(54320);
        time.Advance(TimeSpan.FromSeconds(2));
        await fixture.WaitForStatusAsync(first.Operation.OperationId, GateOperationStatus.TimedOut);

        var second = await OpenEntryAsync(54320);
        time.Advance(TimeSpan.FromSeconds(2));
        await fixture.WaitForStatusAsync(second.Operation.OperationId, GateOperationStatus.TimedOut);

        var rejected = await OpenEntryAsync(54320);
        var rejection = MakeAck(rejected.Frame);
        rejection[11] = 3;
        await PublishAckAsync(simulator, "ack/network/54320", rejection);
        await fixture.WaitForStatusAsync(rejected.Operation.OperationId, GateOperationStatus.Failed);

        await using (var db = fixture.OpenDb())
        {
            var before = await db.Set<Ticket>().SingleAsync(x => x.Id == ticket.Id);
            Assert.Equal((EntryConfirmationStatus.Pending, BasePaymentStatus.Paid, ticket.StartDate, ticket.UniqueCode, ticket.Price),
                (before.EntryConfirmationStatus, before.BasePaymentStatus, before.StartDate, before.UniqueCode, before.Price));
            Assert.Equal(qr, await fixture.GetTicketQrAsync(ticket.Id));
            var attempts = await db.Set<GateOperation>().ToListAsync();
            Assert.Equal(3, attempts.Select(x => x.CommandId).Distinct().Count());
            Assert.All(attempts, op =>
            {
                Assert.Null(op.ReservedControllerId);
                Assert.Null(op.ReservedTicketId);
                Assert.Equal(ticket.Id, op.TicketIdSnapshot);
            });
            var gate = await db.Set<ParkingGate>().SingleAsync(x => x.Id == seed.EntryGate);
            gate.SetControlIp("54319");
            await db.SaveChangesAsync();
        }

        var remapped = await OpenEntryAsync(54319);
        await PublishAckAsync(simulator, "ack/network/54319", MakeAck(remapped.Frame));
        await fixture.WaitForStatusAsync(remapped.Operation.OperationId, GateOperationStatus.Completed);

        var manualRequest = new OpenGateCommand { ControllerId = "1", OperationType = "Manual" };
        for (byte code = 1; code <= 2; code++)
        {
            var manual = await fixture.SendAsync(manualRequest, seed.ExitGate, seed.Supervisor, "Supervisor");
            manual.ThrowIfFailed();
            var frame = await ReadFrameAsync(frames.Reader);
            AssertOpen(frame, manual.Value, 1);
            var ack = MakeAck(frame);
            ack[11] = code;
            await PublishAckAsync(simulator, "ack/network/1", ack);
            await fixture.WaitForStatusAsync(manual.Value.OperationId, GateOperationStatus.Failed);
            await using var db = fixture.OpenDb();
            var row = await db.Set<GateOperation>().SingleAsync(x => x.Id == manual.Value.OperationId);
            Assert.Equal(code, row.AckStatusCode);
            Assert.Null(row.ReservedControllerId);
            Assert.Null(row.ReservedTicketId);
        }

        var withheld = await fixture.SendAsync(manualRequest, seed.ExitGate, seed.Supervisor, "Supervisor");
        withheld.ThrowIfFailed();
        AssertOpen(await ReadFrameAsync(frames.Reader), withheld.Value, 1);
        await fixture.WaitForStatusAsync(withheld.Value.OperationId, GateOperationStatus.Published);
        time.Advance(TimeSpan.FromSeconds(2));
        await fixture.WaitForStatusAsync(withheld.Value.OperationId, GateOperationStatus.TimedOut);

        using var exitCashier = await fixture.CreateHttpClientAsync(seed.ExitGate, seed.ExitCashier);
        var exit = await RequestExitAsync(exitCashier, new ExitTicketCommand { TicketId = seed.TicketId });
        var exitFrame = await ReadFrameAsync(frames.Reader);
        AssertOpen(exitFrame, exit, 1);
        await PublishAckAsync(simulator, "ack/network/1", MakeAck(exitFrame));
        await fixture.WaitForStatusAsync(exit.OperationId, GateOperationStatus.Completed);
        await using var verified = fixture.OpenDb();
        var confirmed = Assert.Single(await verified.Set<Ticket>().ToListAsync());
        Assert.Equal(ticket.Id, confirmed.Id);
        Assert.Equal(EntryConfirmationStatus.Confirmed, confirmed.EntryConfirmationStatus);
        Assert.Equal(qr, await fixture.GetTicketQrAsync(ticket.Id));
        Assert.Empty(await verified.Set<TicketExceedTransaction>().ToListAsync());
        Assert.False(frames.Reader.TryRead(out _));
    }

    [SqlBrokerFact]
    public async Task AtTheAckDeadlineBothAnEarlyReadAndAConcurrentTimeoutExcludeLateAckSuccess()
    {
        var time = new ManualParkingTime(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
        await using var fixture = await Fixture.StartAsync(time, recoveryIntervalSeconds: 60);
        var seed = await fixture.SeedAsync(sameDay: true, exitController: "1");
        var direct = new GateOperation(Guid.NewGuid(), seed.TicketId, seed.ExitGate, seed.ExitCashier, 1,
            GateOperationType.Exit, fixture.Clock.UtcNow);
        var racing = new GateOperation(Guid.NewGuid(), null, seed.ExitGate, seed.Supervisor, 2,
            GateOperationType.Manual, fixture.Clock.UtcNow);
        var valid = new GateOperation(Guid.NewGuid(), null, seed.ExitGate, seed.Supervisor, 3,
            GateOperationType.Manual, fixture.Clock.UtcNow);
        await using (var db = fixture.OpenDb())
        {
            foreach (var op in new[] { direct, racing, valid })
            {
                op.BeginDispatch(fixture.Clock.UtcNow);
                op.MarkPublished(fixture.Clock.UtcNow);
            }
            db.AddRange(direct, racing, valid);
            await db.SaveChangesAsync();
        }

        time.Advance(TimeSpan.FromMilliseconds(1999));
        (await RecordAckAsync(valid)).ThrowIfFailed();
        time.Advance(TimeSpan.FromMilliseconds(1));
        (await RecordAckAsync(direct)).ThrowIfFailed();
        var ack = RecordAckAsync(racing);
        var timeout = fixture.SendAsync(new AdvanceGateOperation(racing.Id, GateTransition.CheckTimeout),
            seed.ExitGate, seed.Supervisor, "Supervisor");
        await Task.WhenAll(ack, timeout);
        Assert.True(ack.Result.IsSuccess || timeout.Result.IsSuccess);

        await using var verified = fixture.OpenDb();
        var states = await verified.Set<GateOperation>().ToDictionaryAsync(x => x.Id);
        Assert.Equal(GateOperationStatus.Acknowledged, states[valid.Id].Status);
        Assert.Equal((byte)0, states[valid.Id].AckStatusCode);
        Assert.Null(states[valid.Id].ReservedControllerId);
        foreach (var id in new[] { direct.Id, racing.Id })
        {
            var row = states[id];
            Assert.Equal(GateOperationStatus.TimedOut, row.Status);
            Assert.Null(row.AckStatusCode);
            Assert.Null(row.ReservedControllerId);
            Assert.Null(row.ReservedTicketId);
        }
        Assert.Empty(await verified.Set<TicketArchive>().ToListAsync());

        Task<BuildingBlock.Domain.Results.Result<GateTransitionResult>> RecordAckAsync(GateOperation operation) =>
            fixture.SendAsync(new RecordGateAcknowledgment(new GateAck((ushort)operation.ControllerId, (ulong)operation.CommandId, 0)),
                seed.ExitGate, seed.Supervisor, "Supervisor");
    }

    [SqlBrokerFact]
    public async Task AnExpiringInFlightPublishSettlesBeforeReleaseEvenWhenSignalRThrows()
    {
        var time = new ManualParkingTime(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
        var publisher = new DelayedPublisher();
        await using var fixture = await Fixture.StartAsync(time, publisher: publisher, failNotifications: true);
        var seed = await fixture.SeedAsync(sameDay: true, exitController: "1");
        using var cashier = await fixture.CreateHttpClientAsync(seed.ExitGate, seed.ExitCashier);
        var attempt = await RequestExitAsync(cashier, new ExitTicketCommand { TicketId = seed.TicketId });
        try
        {
            await publisher.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            time.Advance(TimeSpan.FromSeconds(3));
            await publisher.Canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await using var claiming = fixture.OpenDb();
            var row = await claiming.Set<GateOperation>().SingleAsync();
            Assert.Equal((GateOperationStatus.Pending, 1, seed.TicketId), (row.Status, row.ReservedControllerId, row.ReservedTicketId));
            Assert.NotNull(row.DispatchStartedAtUtc);
        }
        finally
        {
            publisher.Settle.TrySetResult();
        }

        await fixture.WaitForStatusAsync(attempt.OperationId, GateOperationStatus.Unknown);
        await fixture.Logs.WaitForAsync(log => log.Contains("Injected SignalR notification failure"));
        Assert.False(await publisher.PastDeadlineSendAllowed.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        await using var verified = fixture.OpenDb();
        var released = await verified.Set<GateOperation>().SingleAsync();
        Assert.Equal("GateDeliveryUnknown", released.ErrorCode);
        Assert.Null(released.ReservedControllerId);
        Assert.Null(released.ReservedTicketId);
        Assert.Null(released.AckStatusCode);
        Assert.NotNull(released.DispatchStartedAtUtc);
        Assert.Empty(await verified.Set<TicketArchive>().ToListAsync());
    }

    private sealed class DelayedPublisher : IMqttPublisher
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Settle { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> PastDeadlineSendAllowed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task EnsureConnectedAsync(CancellationToken ct) => Task.CompletedTask;

        public Task PublishStringAsync(string topic, string payload, MqttPublishOptions options, CancellationToken ct) =>
            throw new NotSupportedException();

        public async Task PublishBytesAsync(string topic, ReadOnlyMemory<byte> payload, MqttPublishOptions options,
            CancellationToken ct, Func<bool>? canPublish)
        {
            using var registration = ct.Register(() => Canceled.TrySetResult());
            Entered.TrySetResult();
            await Settle.Task.WaitAsync(TimeSpan.FromSeconds(20));
            PastDeadlineSendAllowed.TrySetResult(canPublish?.Invoke() == true);
            ct.ThrowIfCancellationRequested();
        }
    }
}
