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

    public static bool TryDecodeAck(string topic, ReadOnlySpan<byte> bytes, out GateAck ack)
    {
        ack = default;
        const string prefix = "ack/network/";
        if (bytes.Length != 13 || bytes[0] != 0x7B || bytes[12] != 0x7D || bytes[11] > 3 ||
            !topic.StartsWith(prefix, StringComparison.Ordinal) ||
            !ushort.TryParse(topic.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var controller) ||
            controller == 0 || BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(1, 2)) != controller ||
            !topic.AsSpan(prefix.Length).SequenceEqual(controller.ToString(CultureInfo.InvariantCulture)))
            return false;

        var command = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(3, 8));
        if (command == 0)
            return false;
        ack = new(controller, command, bytes[11]);
        return true;
    }
}
