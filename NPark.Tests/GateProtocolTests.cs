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

    [Fact]
    public void HandlesLateAckWithoutRegressingOnPublishOrDuplicatingCompletion()
    {
        var now = new DateTime(2026, 11, 24, 13, 0, 0, DateTimeKind.Utc);
        var operation = new GateOperation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            GateOperationType.Exit, now);
        Assert.False(operation.Acknowledge(0, now));
        operation.BeginDispatch(now, retry: false);
        operation.MarkUncertain(timeout: true);
        Assert.NotNull(operation.ReservedTicketId);
        Assert.True(operation.Acknowledge(0, now.AddSeconds(4)));
        operation.MarkPublished(now.AddSeconds(5));
        Assert.Equal(GateOperationStatus.Acknowledged, operation.Status);
        operation.Complete(now.AddSeconds(8));
        Assert.False(operation.Acknowledge(0, now.AddSeconds(9)));
        Assert.Equal(now.AddSeconds(4), operation.AcknowledgedAtUtc);
        Assert.Null(operation.ReservedControllerId);
    }
}
