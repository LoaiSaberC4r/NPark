using System.Buffers.Binary;
using System.Globalization;

namespace NPark.Application.Services;

public readonly record struct GateAck(ushort ControllerId, ulong CommandId, byte Status);

public static class GatePacketCodec
{
    public static byte[] EncodeOpen(ushort controller, ulong commandId)
    {
        ArgumentOutOfRangeException.ThrowIfZero(controller);
        ArgumentOutOfRangeException.ThrowIfZero(commandId);
        byte[] bytes = [0x7B, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xAE, 0xBE, 0xC1, 0xB1, 0x7D];
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(1, 2), controller);
        BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(3, 8), commandId);
        return bytes;
    }

    public static bool TryDecodeAck(string topic, ReadOnlySpan<byte> bytes, out GateAck ack) =>
        TryDecodeAck(topic, bytes, out ack, out _);

    public static bool TryDecodeAck(string topic, ReadOnlySpan<byte> bytes, out GateAck ack, out string? reason)
    {
        ack = default;
        reason = "MalformedFrame";
        if (bytes.Length != 13 || bytes[0] != 0x7B || bytes[12] != 0x7D)
            return false;

        ack = new(BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(1, 2)),
            BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(3, 8)), bytes[11]);
        reason = "InvalidFrameFields";
        if (ack.ControllerId == 0 || ack.CommandId == 0 || ack.Status > 3)
            return false;

        const string prefix = "ack/network/";
        reason = "ControllerTopicMismatch";
        if (!topic.StartsWith(prefix, StringComparison.Ordinal) ||
            !topic.AsSpan(prefix.Length).SequenceEqual(ack.ControllerId.ToString(CultureInfo.InvariantCulture)))
            return false;

        reason = null;
        return true;
    }
}
