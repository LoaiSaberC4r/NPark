using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPark.Application.Abstraction;
using NPark.Application.Options;
using NPark.Application.Services;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Services.Mqtt;

internal sealed class ParkingEventStorage(IServiceScopeFactory scopes, IMqttPublisher publisher,
    IOptions<AutomaticVisitorEntryOptions> entryOptions, TimeProvider time, ILogger<ParkingEventStorage> logger)
{
    public async Task StoreAndAcknowledgeAsync(ParkingEventPacket packet, byte[] payload, DateTime receivedAtUtc,
        CancellationToken ct)
    {
        using var logging = logger.BeginScope(new Dictionary<string, object>
        {
            ["ControllerId"] = packet.ControllerId,
            ["EventId"] = Convert.ToHexString(packet.EventId),
            ["ReceivedAtUtc"] = receivedAtUtc
        });

        ParkingHardwareEvent stored;
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
            var existing = await FindAsync(db, packet, ct);
            if (existing is null)
            {
                var clock = scope.ServiceProvider.GetRequiredService<ParkingBusinessClock>();
                stored = new ParkingHardwareEvent(packet.ControllerId, packet.EventId, packet.Schema, packet.Lane,
                    packet.Outcome, packet.Flags, packet.CardNumber, packet.RawTimestamp,
                    DecodeHardwareTime(packet, clock), receivedAtUtc, payload);
                if (!entryOptions.Value.Enabled)
                    stored.Finish(ParkingEventProcessingStatus.Ignored, "AutomaticVisitorEntryDisabled", clock.UtcNow);

                db.ParkingHardwareEvents.Add(stored);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Parking event stored. EventRecordId={EventRecordId}, Schema={Schema}, " +
                    "Lane={Lane}, Outcome={Outcome}, Flags={Flags}, ProcessingStatus={ProcessingStatus}",
                    stored.Id, stored.Schema, stored.Lane, stored.Outcome, stored.Flags, stored.ProcessingStatus);
            }
            else
            {
                stored = existing;
            }
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
            stored = await FindAsync(db, packet, ct) ?? throw new InvalidOperationException(
                "The conflicting parking event was not found after the insert failed.", error);
        }

        if (!stored.RawPayload.AsSpan().SequenceEqual(payload))
        {
            logger.LogWarning("Parking event ID has a conflicting payload. EventRecordId={EventRecordId}, ReasonCode={ReasonCode}",
                stored.Id, "ParkingEventPayloadConflict");
            return;
        }
        if (stored.StorageAckSentAtUtc.HasValue || stored.ReceivedAtUtc.Ticks != receivedAtUtc.Ticks)
            logger.LogDebug("Duplicate parking event. EventRecordId={EventRecordId}", stored.Id);

        await AcknowledgeAsync(stored, ct);
    }

    public async Task AcknowledgeAsync(ParkingHardwareEvent stored, CancellationToken ct)
    {
        var payload = ParkingEventPacketCodec.EncodeStorageAck(stored.RawPayload);
        var topic = ParkingEventPacketCodec.StorageAckTopic(stored.ControllerId);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(TimeSpan.FromSeconds(2));
        await publisher.PublishBytesAsync(topic, payload, new MqttPublishOptions(), budget.Token);
        logger.LogDebug("MQTT parking storage ACK published. Direction={Direction}, Topic={Topic}, PayloadLength={PayloadLength}, " +
            "PayloadHex={PayloadHex}, Qos={Qos}, Retain={Retain}, EventRecordId={EventRecordId}",
            "Outbound", topic, payload.Length, Convert.ToHexString(payload), 1, false, stored.Id);

        if (stored.StorageAckSentAtUtc is null)
        {
            var sentAt = time.GetUtcNow().UtcDateTime;
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
            await db.ParkingHardwareEvents.Where(x => x.Id == stored.Id && x.StorageAckSentAtUtc == null)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.StorageAckSentAtUtc, sentAt), ct);
        }
    }

    private static Task<ParkingHardwareEvent?> FindAsync(NParkDBContext db, ParkingEventPacket packet, CancellationToken ct) =>
        db.ParkingHardwareEvents.AsNoTracking().SingleOrDefaultAsync(
            x => x.ControllerId == packet.ControllerId && x.EventId == packet.EventId, ct);

    private static DateTime? DecodeHardwareTime(ParkingEventPacket packet, ParkingBusinessClock clock)
    {
        if ((packet.Flags & 0x11) != 0x11 || packet.RawTimestamp == 0)
            return null;

        var local = DateTimeOffset.FromUnixTimeSeconds(packet.RawTimestamp).DateTime;
        return clock.TryConvertHardwareTime(local);
    }
}
