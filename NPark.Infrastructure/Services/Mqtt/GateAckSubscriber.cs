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
    private volatile bool ready;
    private int reconnectRequested;
    public bool Ready { get => ready; set => ready = value; }
    public void RequestReconnect()
    {
        Ready = false;
        Interlocked.Exchange(ref reconnectRequested, 1);
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
        using var client = new MqttFactory().CreateMqttClient();
        client.DisconnectedAsync += _ =>
        {
            subscription.Ready = false;
            return Task.CompletedTask;
        };
        client.ApplicationMessageReceivedAsync += args => ReceiveAsync(args, ct);
        var builder = new MqttClientOptionsBuilder().WithClientId($"{broker.ClientId}-gate-ack")
            .WithTcpServer(broker.Host, broker.Port).WithProtocolVersion(MqttProtocolVersion.V311)
            .WithCleanSession(false);
        if (!string.IsNullOrWhiteSpace(broker.Username))
            builder.WithCredentials(broker.Username, broker.Password);
        if (broker.UseTls)
            builder.WithTlsOptions(tls => tls.UseTls().WithSslProtocols(SslProtocols.Tls12));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (subscription.TakeReconnectRequest() && client.IsConnected)
                    await client.DisconnectAsync(cancellationToken: ct);
                if (!subscription.Ready)
                {
                    if (!client.IsConnected)
                        await client.ConnectAsync(builder.Build(), ct);

                    var filters = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(f => f
                        .WithTopic(gate.AckTopicFilter).WithQualityOfServiceLevel((MqttQualityOfServiceLevel)gate.AckQos)).Build();
                    var subscribed = await client.SubscribeAsync(filters, ct);
                    subscription.Ready = client.IsConnected && subscribed.Items.Any(item => (int)item.ResultCode < 128);
                }
            }
            catch (Exception error) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(error, "Gate ACK subscription is unavailable");
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, broker.ReconnectSeconds)), ct);
        }
    }

    private async Task ReceiveAsync(MqttApplicationMessageReceivedEventArgs args, CancellationToken ct)
    {
        var message = args.ApplicationMessage;
        if (message.Retain || !GatePacketCodec.TryDecodeAck(message.Topic, message.PayloadSegment.AsSpan(), out var ack))
            return;

        args.AutoAcknowledge = false;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var scope = scopes.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new RecordGateAcknowledgment(ack), ct);
            if (result.IsSuccess)
            {
                await args.AcknowledgeAsync(ct);
                await dispatcher.PublishStatusAsync(result.Value);
                return;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), ct);
        }
        logger.LogError("Gate ACK could not be persisted for controller {ControllerId}, command {CommandId}",
            ack.ControllerId, ack.CommandId);
        subscription.RequestReconnect();
    }
}
