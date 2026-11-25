using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using NPark.Application.Options;
using NPark.Application.Services;
using NPark.Infrastructure.Options;
using System.Security.Authentication;

namespace NPark.Infrastructure.Services.Mqtt;

internal sealed class GateAckSubscriptionState
{
    private volatile IMqttClient? subscribedClient;
    private int reconnectRequested;
    public bool Ready => subscribedClient?.IsConnected == true && Volatile.Read(ref reconnectRequested) == 0;
    public void Confirm(IMqttClient client) => subscribedClient = client;
    public void Clear() => subscribedClient = null;
    public void RequestReconnect()
    {
        Interlocked.Exchange(ref reconnectRequested, 1);
        Clear();
    }
    public bool TakeReconnectRequest() => Interlocked.Exchange(ref reconnectRequested, 0) == 1;
}

internal sealed class GateAckSubscriber(IServiceScopeFactory scopes, IOptions<MqttOptions> brokerOptions,
    IOptions<GateOperationOptions> gateOptions, GateAckSubscriptionState subscription,
    IGateOperationDispatcher dispatcher, ILogger<GateAckSubscriber> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var broker = brokerOptions.Value;
        var gate = gateOptions.Value;
        // Keep the session ID across reconnects and restarts. Each worker needs its own configured base ID.
        var clientId = $"{broker.ClientId}-gate-ack";
        using var client = new MqttFactory().CreateMqttClient();
        client.DisconnectedAsync += args =>
        {
            subscription.Clear();
            if (args.ClientWasConnected && !ct.IsCancellationRequested)
                logger.LogWarning("Gate ACK subscriber disconnected. ClientId={ClientId}, TopicFilter={TopicFilter}, " +
                    "AckSubscriptionReady={AckSubscriptionReady}, Reason={Reason}", clientId, gate.AckTopicFilter, false, args.Reason);
            return Task.CompletedTask;
        };
        client.ApplicationMessageReceivedAsync += args => ReceiveAsync(args, ct);
        var builder = new MqttClientOptionsBuilder().WithClientId(clientId)
            .WithTcpServer(broker.Host, broker.Port).WithProtocolVersion(MqttProtocolVersion.V311)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(broker.KeepAliveSeconds))
            .WithCleanSession(false);
        if (!string.IsNullOrWhiteSpace(broker.Username))
            builder.WithCredentials(broker.Username, broker.Password);
        if (broker.UseTls)
            builder.WithTlsOptions(tls => tls.UseTls().WithSslProtocols(SslProtocols.Tls12));

        var connection = builder.Build();
        var filters = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(f => f
            .WithTopic(gate.AckTopicFilter).WithQualityOfServiceLevel((MqttQualityOfServiceLevel)gate.AckQos)).Build();
        logger.LogInformation("Gate ACK subscriber configuration. Host={Host}, Port={Port}, ClientId={ClientId}, " +
            "TopicFilter={TopicFilter}, Qos={Qos}", broker.Host, broker.Port, clientId, gate.AckTopicFilter, gate.AckQos);
        var outageLogged = false;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (subscription.TakeReconnectRequest() && client.IsConnected)
                        await client.DisconnectAsync(cancellationToken: ct);
                    if (!subscription.Ready)
                    {
                        subscription.Clear();
                        if (!client.IsConnected)
                        {
                            var connected = await client.ConnectAsync(connection, ct);
                            outageLogged = false;
                            logger.LogInformation("Gate ACK subscriber connected. Host={Host}, Port={Port}, " +
                                "ClientId={ClientId}, SessionPresent={SessionPresent}", broker.Host, broker.Port,
                                clientId, connected.IsSessionPresent);
                        }

                        var subscribed = await client.SubscribeAsync(filters, ct);
                        var granted = subscribed.Items.SingleOrDefault(item => item.TopicFilter.Topic == gate.AckTopicFilter);
                        // QoS 0 cannot preserve acknowledgments for redelivery by the persistent session.
                        if (granted is null || granted.ResultCode is not
                            (MqttClientSubscribeResultCode.GrantedQoS1 or MqttClientSubscribeResultCode.GrantedQoS2))
                            throw new InvalidOperationException($"Unusable gate ACK SUBACK: {granted?.ResultCode}.");
                        if (client.IsConnected)
                        {
                            subscription.Confirm(client);
                            if (subscription.Ready)
                            {
                                outageLogged = false;
                                logger.LogInformation("Gate ACK subscription confirmed. TopicFilter={TopicFilter}, " +
                                    "ClientId={ClientId}, GrantedQos={GrantedQos}, AckSubscriptionReady={AckSubscriptionReady}",
                                    gate.AckTopicFilter, clientId, granted.ResultCode, true);
                            }
                        }
                    }
                }
                catch (Exception error) when (!ct.IsCancellationRequested)
                {
                    subscription.Clear();
                    if (!outageLogged)
                    {
                        logger.LogWarning(error, "Gate ACK subscription unavailable. Host={Host}, Port={Port}, " +
                            "ClientId={ClientId}, TopicFilter={TopicFilter}, AckSubscriptionReady={AckSubscriptionReady}, " +
                            "ReconnectSeconds={ReconnectSeconds}", broker.Host, broker.Port, clientId,
                            gate.AckTopicFilter, false, broker.ReconnectSeconds);
                        outageLogged = true;
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(broker.ReconnectSeconds), ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        finally
        {
            subscription.Clear();
        }
    }

    private async Task ReceiveAsync(MqttApplicationMessageReceivedEventArgs args, CancellationToken ct)
    {
        var message = args.ApplicationMessage;
        var payload = message.PayloadSegment;
        logger.LogInformation("Hardware ACK received. Topic={Topic}, PayloadHex={PayloadHex}, PayloadLength={PayloadLength}",
            message.Topic, Convert.ToHexString(payload.AsSpan()[..Math.Min(payload.Count, 32)]), payload.Count);
        var decoded = GatePacketCodec.TryDecodeAck(message.Topic, payload.AsSpan(), out var ack, out var reason);
        if (message.Retain || !decoded)
        {
            logger.LogWarning("Hardware ACK ignored. Reason={Reason}, Topic={Topic}, ControllerId={ControllerId}, CommandId={CommandId}",
                message.Retain ? "Retained" : reason, message.Topic, ack.ControllerId, ack.CommandId);
            return;
        }

        logger.LogInformation("Hardware ACK decoded. ControllerId={ControllerId}, CommandId={CommandId}, Status={AckStatusCode}",
            ack.ControllerId, ack.CommandId, ack.Status);
        args.AutoAcknowledge = false;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            GateTransitionResult? change = null;
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RecordGateAcknowledgment(ack), ct);
                if (result.IsSuccess)
                    change = result.Value;
                else
                    logger.LogWarning("Hardware ACK persistence attempt failed. ControllerId={ControllerId}, " +
                        "CommandId={CommandId}, Attempt={Attempt}, Codes={Codes}", ack.ControllerId, ack.CommandId,
                        attempt, string.Join(",", result.Errors.Select(e => e.Code)));
            }
            catch (Exception error) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(error, "Hardware ACK persistence attempt failed. ControllerId={ControllerId}, " +
                    "CommandId={CommandId}, Attempt={Attempt}", ack.ControllerId, ack.CommandId, attempt);
            }
            if (change is not null)
            {
                if (change.Operation is { } operation)
                    logger.LogInformation("Hardware ACK persisted. OperationId={OperationId}, ControllerId={ControllerId}, " +
                        "CommandId={CommandId}, AckStatusCode={AckStatusCode}, AcknowledgedAtUtc={AcknowledgedAtUtc}",
                        operation.OperationId, ack.ControllerId, ack.CommandId, ack.Status, operation.AcknowledgedAtUtc);
                try
                {
                    await args.AcknowledgeAsync(ct);
                }
                catch (Exception error) when (!ct.IsCancellationRequested)
                {
                    logger.LogWarning(error, "MQTT ACK confirmation failed after processing command {CommandId}", ack.CommandId);
                    subscription.RequestReconnect();
                }
                await dispatcher.PublishStatusAsync(change);
                return;
            }
            if (attempt < 3)
                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), ct);
        }
        logger.LogError("Gate ACK could not be persisted for controller {ControllerId}, command {CommandId}",
            ack.ControllerId, ack.CommandId);
        subscription.RequestReconnect();
    }
}
