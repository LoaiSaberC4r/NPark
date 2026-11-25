using NPark.Application.Services;
using NPark.Domain.Enums;
using Xunit;

namespace NPark.Tests;

public sealed class ParkingEventProtocolTests
{
    private const string Visitor = "7B000100000315000000010101011D6B0527E0047B007DAB7D";

    [Fact]
    public void EncodesOpenAndLockWithTheOriginalCommandLayout()
    {
        Assert.Equal("7B00010000000000000065AEBEC1B17D", Convert.ToHexString(GatePacketCodec.EncodeOpen(1, 101)));
        Assert.Equal("7B00010000000000000065AEBEC1B27D", Convert.ToHexString(
            GatePacketCodec.EncodeOperation(GateOperationType.LockCardButton, 1, 101)));
        Assert.Equal("7BFFFFFEDCBA9876543210AEBEC1B27D", Convert.ToHexString(
            GatePacketCodec.EncodeLock(ushort.MaxValue, 0xFEDCBA9876543210)));
        Assert.True(GatePacketCodec.TryDecodeAck("ack/network/1", Convert.FromHexString("7B00010000000000000065007D"),
            out var ack));
        Assert.Equal(new GateAck(1, 101, 0), ack);
    }

    [Theory]
    [InlineData(Visitor, "7B007DAB", true)]
    [InlineData("7B000100000315000000010101011D6B0527E004007B7D007D", "007B7D00", true)]
    [InlineData("7B000100000315000000010101011D6B0527E0007D", null, false)]
    [InlineData("7B000100000315000000010102011D6B0527E0047B007DAB7D", "7B007DAB", false)]
    [InlineData("7B000100000315000000010101040000000000007D", null, false)]
    [InlineData("7B00010000031500000002020101116B0527E0047B007DAB6B06AE80020F7D", "7B007DAB", false)]
    public void ParsesOffsetsWithoutConflatingStorageAndVisitorEligibility(string hex, string? uid, bool eligible)
    {
        var bytes = Convert.FromHexString(hex);
        Assert.True(ParkingEventPacketCodec.TryDecode("parking/v1/event/1", bytes, out var frame, out var reason), reason);
        Assert.Equal(uid, frame.CardNumber);
        Assert.Equal(eligible, frame.IsVisitorCardPickup);
        Assert.Equal(bytes[3..11], frame.EventId);
        var ack = ParkingEventPacketCodec.EncodeStorageAck(bytes);
        Assert.Equal(13, ack.Length);
        Assert.Equal(bytes[..11], ack[..11]);
        Assert.Equal(new byte[] { 0, 0x7D }, ack[11..]);
        if (hex == Visitor)
            Assert.Equal("7B00010000031500000001007D", Convert.ToHexString(ack));
    }

    [Theory]
    [InlineData("parking/v1/event/01", Visitor)]
    [InlineData("parking/v1/event/2", Visitor)]
    [InlineData("parking/v1/event/1", "7B000100000315000000010101011D6B0527E0037B007DAB7D")]
    [InlineData("parking/v1/event/1", "7B000100000315000000010101011D6B0527E0047B007D7D")]
    [InlineData("parking/v1/event/1", "7B000100000315000000010201011D6B0527E0047B007DAB7D")]
    public void RejectsMalformedFramesOrTopicIdentity(string topic, string hex) =>
        Assert.False(ParkingEventPacketCodec.TryDecode(topic, Convert.FromHexString(hex), out _, out _));
}
