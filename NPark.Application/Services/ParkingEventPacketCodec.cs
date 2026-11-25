using System.Buffers.Binary;
using System.Globalization;

namespace NPark.Application.Services;

public readonly record struct ParkingEventPacket(ushort ControllerId, byte[] EventId, byte Schema,
    byte Lane, byte Outcome, byte Flags, uint RawTimestamp, string? CardNumber)
{
    public bool IsVisitorCardPickup => Schema == 1 && Lane == 1 && Outcome == 1 && CardNumber is not null;
}

public static class ParkingEventPacketCodec
{
    public const string EventTopicFilter = "parking/v1/event/+";
    private const string EventTopicPrefix = "parking/v1/event/";

    public static string StorageAckTopic(int controllerId) =>
        "parking/v1/ack/" + controllerId.ToString(CultureInfo.InvariantCulture);

    public static bool TryDecode(string topic, ReadOnlySpan<byte> bytes, out ParkingEventPacket packet,
        out string? reason)
    {
        packet = default;
        reason = "MalformedFrame";
        if (bytes.Length < 21 || bytes[0] != 0x7B || bytes[^1] != 0x7D)
            return false;

        var controllerId = BinaryPrimitives.ReadUInt16BigEndian(bytes[1..3]);
        var schema = bytes[11];
        var lane = bytes[12];
        var outcome = bytes[13];
        var uidLength = bytes[19];
        if (controllerId == 0 || uidLength is not (0 or 4) ||
            (schema switch
            {
                1 => bytes.Length != 21 + uidLength || lane is not (1 or 2) ||
                    (lane == 1 ? outcome is not (1 or 4) : outcome is < 1 or > 4),
                2 => bytes.Length != 27 + uidLength || lane != 1 || outcome is not (1 or 4),
                _ => true
            }))
            return false;

        reason = "ControllerTopicMismatch";
        if (!topic.StartsWith(EventTopicPrefix, StringComparison.Ordinal) ||
            !topic.AsSpan(EventTopicPrefix.Length).SequenceEqual(controllerId.ToString(CultureInfo.InvariantCulture)))
            return false;

        packet = new(controllerId, bytes[3..11].ToArray(), schema, lane, outcome, bytes[14],
            BinaryPrimitives.ReadUInt32BigEndian(bytes[15..19]),
            uidLength == 0 ? null : Convert.ToHexString(bytes.Slice(20, uidLength)));
        reason = null;
        return true;
    }

    public static byte[] EncodeStorageAck(ReadOnlySpan<byte> validatedEvent)
    {
        var ack = new byte[13];
        validatedEvent[..11].CopyTo(ack);
        ack[12] = 0x7D;
        return ack;
    }
}
