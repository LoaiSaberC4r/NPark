using System.Security.Authentication;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using NPark.Application.Services;
using NPark.Infrastructure.Options;

namespace NPark.Infrastructure.Services.Mqtt;

internal sealed class MqttParkingEventSubscriber(IOptions<MqttOptions> brokerOptions,
    IOptions<MqttParkingEventsOptions> options, TimeProvider time, ParkingEventStorage storage,
    ILogger<MqttParkingEventSubscriber> logger) : BackgroundService
{
    private sealed record Arrival(string Topic, byte[] Payload, int Qos, DateTime ReceivedAtUtc);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!options.Value.Enabled)
            return;

        var queue = Channel.CreateBounded<Arrival>(new BoundedChannelOptions(256)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        using var client = new MqttFactory().CreateMqttClient();
        client.ApplicationMessageReceivedAsync += args =>
        {
            var message = args.ApplicationMessage;
            if (!message.Retain && message.PayloadSegment.Count is >= 21 and <= 31)
            {
                var receivedAtUtc = time.GetUtcNow().UtcDateTime;
                if (!queue.Writer.TryWrite(new(message.Topic, message.PayloadSegment.ToArray(),
                    (int)message.QualityOfServiceLevel, receivedAtUtc)))
                    logger.LogWarning("MQTT parking event intake queue full. Topic={Topic}", message.Topic);
            }
            else
            {
                logger.LogDebug("MQTT parking event discarded. Topic={Topic}, PayloadLength={PayloadLength}, Retain={Retain}",
                    message.Topic, message.PayloadSegment.Count, message.Retain);
            }
            return Task.CompletedTask;
        };
        await Task.WhenAll(MaintainSubscriptionAsync(client, ct), ConsumeAsync(queue.Reader, ct));
    }

    private async Task MaintainSubscriptionAsync(IMqttClient client, CancellationToken ct)
    {
        var broker = brokerOptions.Value;
        var clientId = $"{broker.ClientId}-parking-events";
        var builder = new MqttClientOptionsBuilder().WithClientId(clientId)
            .WithTcpServer(broker.Host, broker.Port).WithProtocolVersion(MqttProtocolVersion.V311)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(broker.KeepAliveSeconds)).WithCleanSession();
        if (!string.IsNullOrWhiteSpace(broker.Username))
            builder.WithCredentials(broker.Username, broker.Password);
        if (broker.UseTls)
            builder.WithTlsOptions(tls => tls.UseTls().WithSslProtocols(SslProtocols.Tls12));

        var connection = builder.Build();
        var topic = ParkingEventPacketCodec.EventTopicFilter;
        var filter = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(topic)
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)options.Value.SubscriptionQos)).Build();
        var subscribed = false;
        client.DisconnectedAsync += args =>
        {
            if (args.ClientWasConnected && !ct.IsCancellationRequested)
                logger.LogWarning("MQTT parking event subscriber disconnected. ClientId={ClientId}, Reason={Reason}",
                    clientId, args.Reason);
            return Task.CompletedTask;
        };
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    subscribed = false;
                    var result = await client.ConnectAsync(connection, ct);
                    if (result.ResultCode != MqttClientConnectResultCode.Success)
                        throw new InvalidOperationException($"MQTT parking subscriber connection rejected: {result.ResultCode}.");
                }
                if (!subscribed)
                {
                    var result = await client.SubscribeAsync(filter, ct);
                    var grant = result.Items.Single().ResultCode;
                    if (grant is not (MqttClientSubscribeResultCode.GrantedQoS0 or MqttClientSubscribeResultCode.GrantedQoS1))
                        throw new InvalidOperationException($"MQTT parking event subscription rejected: {grant}.");

                    subscribed = true;
                    logger.LogInformation("MQTT parking event subscription confirmed. Host={Host}, Port={Port}, " +
                        "ClientId={ClientId}, TopicFilter={TopicFilter}, GrantedQos={GrantedQos}",
                        broker.Host, broker.Port, clientId, topic, grant);
                }
            }
            catch (Exception error) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(error, "MQTT parking event subscription unavailable. ClientId={ClientId}, TopicFilter={TopicFilter}",
                    clientId, topic);
            }
            await Task.Delay(TimeSpan.FromSeconds(broker.ReconnectSeconds), ct);
        }
    }

    private async Task ConsumeAsync(ChannelReader<Arrival> queue, CancellationToken ct)
    {
        await foreach (var arrival in queue.ReadAllAsync(ct))
        {
            if (!ParkingEventPacketCodec.TryDecode(arrival.Topic, arrival.Payload, out var packet, out var reason))
            {
                logger.LogWarning("MQTT parking event rejected. Topic={Topic}, PayloadLength={PayloadLength}, ReasonCode={ReasonCode}",
                    arrival.Topic, arrival.Payload.Length, reason);
                continue;
            }

            // The raw, UID-bearing frame is retained in SQL. Logs carry only its non-card identity.
            logger.LogDebug("MQTT parking event received. Direction={Direction}, Topic={Topic}, PayloadLength={PayloadLength}, " +
                "Qos={Qos}, Retain={Retain}, EventId={EventId}",
                "Inbound", arrival.Topic, arrival.Payload.Length, arrival.Qos, false, Convert.ToHexString(packet.EventId));
            try
            {
                await storage.StoreAndAcknowledgeAsync(packet, arrival.Payload, arrival.ReceivedAtUtc, ct);
            }
            catch (Exception error) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(error, "Parking event intake or storage ACK failed. ControllerId={ControllerId}, " +
                    "EventId={EventId}, ReceivedAtUtc={ReceivedAtUtc}",
                    packet.ControllerId, Convert.ToHexString(packet.EventId), arrival.ReceivedAtUtc);
            }
        }
    }
}
