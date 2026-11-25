using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using Xunit;

namespace NPark.Tests;

public sealed class GateProtocolTests
{
    [Fact]
    public void EncodesTheApprovedOpenFrame()
    {
        Assert.Equal("7B0002000000000000002BAEBEC1B17D", Convert.ToHexString(GatePacketCodec.EncodeOpen(2, 43)));
        Assert.Equal("7BFFFF7B7D00000000002BAEBEC1B17D",
            Convert.ToHexString(GatePacketCodec.EncodeOpen(ushort.MaxValue, 0x7B7D00000000002B)));
        Assert.Throws<ArgumentOutOfRangeException>(() => GatePacketCodec.EncodeOpen(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GatePacketCodec.EncodeOpen(1, 0));
    }

    [Theory]
    [InlineData("ack/network/2", "7B0002000000000000002B007D", true)]
    [InlineData("ack/network/2", "7B0002000000000000002B017D", true)]
    [InlineData("ack/network/2", "7B0002000000000000002B027D", true)]
    [InlineData("ack/network/2", "7B0002000000000000002B037D", true)]
    [InlineData("ack/network/3", "7B0002000000000000002B007D", false)]
    [InlineData("ack/network/02", "7B0002000000000000002B007D", false)]
    [InlineData("ack/network/2/extra", "7B0002000000000000002B007D", false)]
    [InlineData("ack/network/2", "000002000000000000002B007D", false)]
    [InlineData("ack/network/2", "7B0002000000000000002B007F", false)]
    [InlineData("ack/network/2", "7B0002000000000000002B047D", false)]
    [InlineData("ack/network/2", "7B00020000000000000000007D", false)]
    [InlineData("ack/network/2", "7B0002000000000000002B007D00", false)]
    [InlineData("ack/network/0", "7B0000000000000000002B007D", false)]
    public void ValidatesFrameAndTopic(string topic, string hex, bool valid)
    {
        Assert.Equal(valid, GatePacketCodec.TryDecodeAck(topic, Convert.FromHexString(hex), out var ack));
        if (valid)
        {
            Assert.Equal((ushort)2, ack.ControllerId);
            Assert.Equal(43UL, ack.CommandId);
        }
    }

    [Theory]
    [InlineData(GateOperationType.Exit, true)]
    [InlineData(GateOperationType.Entry, true)]
    [InlineData(GateOperationType.Manual, true)]
    [InlineData(GateOperationType.Exit, false)]
    public void UncertainAttemptsReleaseReservationsAndStayTerminal(GateOperationType type, bool timeout)
    {
        var now = new DateTime(2026, 11, 24, 13, 0, 0, DateTimeKind.Utc);
        var operation = new GateOperation(Guid.NewGuid(), type == GateOperationType.Manual ? null : Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), 1, type, now);
        Assert.False(operation.Acknowledge(0, now));
        operation.BeginDispatch(now);
        if (timeout)
            operation.MarkPublished(now);
        operation.MarkUncertain(timeout);
        operation.MarkPublished(now.AddSeconds(4));
        Assert.False(operation.Acknowledge(0, now.AddSeconds(4)));
        Assert.False(operation.Acknowledge(3, now.AddSeconds(4)));
        Assert.Throws<InvalidOperationException>(() => operation.BeginDispatch(now.AddSeconds(5)));
        operation.ReconcileNonExecution(Guid.NewGuid(), "Test evidence", now.AddSeconds(6));
        Assert.Equal(timeout ? GateOperationStatus.TimedOut : GateOperationStatus.Unknown, operation.Status);
        Assert.Equal(timeout ? "GateAckTimeout" : "GateDeliveryUnknown", operation.ErrorCode);
        Assert.Equal(now, operation.DispatchStartedAtUtc);
        Assert.Null(operation.AcknowledgedAtUtc);
        Assert.Null(operation.ReservedTicketId);
        Assert.Null(operation.ReservedControllerId);
    }

    [Theory]
    [InlineData(0, GateOperationStatus.Acknowledged, null)]
    [InlineData(1, GateOperationStatus.Failed, "GateExecutionFailed")]
    [InlineData(2, GateOperationStatus.Failed, "GateBusy")]
    [InlineData(3, GateOperationStatus.Failed, "GateRejected")]
    public void HardwareAckReleasesTheControllerAndOnlySuccessKeepsTheTicket(int code, GateOperationStatus status, string? reason)
    {
        var now = DateTime.UnixEpoch;
        var ticket = Guid.NewGuid();
        var operation = new GateOperation(Guid.NewGuid(), ticket, Guid.NewGuid(), null, 1, GateOperationType.Entry, now);
        operation.BeginDispatch(now);
        Assert.True(operation.Acknowledge((byte)code, now));
        operation.MarkPublished(now);
        Assert.False(operation.Acknowledge(0, now.AddSeconds(1)));
        Assert.Equal((status, reason, (byte)code, now), (operation.Status, operation.ErrorCode, operation.AckStatusCode, operation.AcknowledgedAtUtc));
        Assert.Null(operation.ReservedControllerId);
        Assert.Equal(code == 0 ? ticket : (Guid?)null, operation.ReservedTicketId);
        if (code == 0)
        {
            operation.Complete(now.AddSeconds(1));
            Assert.Equal(GateOperationStatus.Completed, operation.Status);
            Assert.Null(operation.ReservedTicketId);
        }
    }
}
