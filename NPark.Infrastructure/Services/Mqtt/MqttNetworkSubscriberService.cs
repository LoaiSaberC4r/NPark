using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using NPark.Application.Abstraction;
using NPark.Application.Feature.MqttNetworkManagement;
using NPark.Application.Feature.MqttNetworkManagement.ProcessMqttExitPacket;
using NPark.Infrastructure.Options;
using System.Security.Authentication;

namespace NPark.Infrastructure.Services.Mqtt
{
    internal sealed class MqttNetworkSubscriberService : BackgroundService
    {
        private readonly ILogger<MqttNetworkSubscriberService> _logger;
        private readonly MqttOptions _mqttOptions;
        private readonly MqttNetworkSubscriberOptions _subscriberOptions;
        private readonly IMqttCaptureSignalRNotifier _notifier;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMqttClient _client;

        private CancellationToken _stoppingToken;

        public MqttNetworkSubscriberService(
            IOptions<MqttOptions> mqttOptions,
            IOptions<MqttNetworkSubscriberOptions> subscriberOptions,
            IMqttCaptureSignalRNotifier notifier,
            IServiceScopeFactory scopeFactory,
            ILogger<MqttNetworkSubscriberService> logger)
        {
            _mqttOptions = mqttOptions.Value;
            _subscriberOptions = subscriberOptions.Value;
            _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = new MqttFactory().CreateMqttClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;

            if (!_subscriberOptions.Enabled)
            {
                _logger.LogInformation("MQTT Network Subscriber is disabled.");
                return;
            }

            ConfigureClientHandlers();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndSubscribeAsync(stoppingToken);
                }
                catch (Exception error) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(error, "MQTT QR capture subscription is unavailable");
                    if (_client.IsConnected)
                        await _client.DisconnectAsync(cancellationToken: stoppingToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _subscriberOptions.ReconnectSeconds)), stoppingToken);
            }
        }

        private void ConfigureClientHandlers()
        {
            _client.ApplicationMessageReceivedAsync += async args =>
            {
                try
                {
                    await HandleMessageAsync(args);
                }
                catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while handling MQTT QR capture.");
                }
            };
        }

        private async Task ConnectAndSubscribeAsync(CancellationToken cancellationToken)
        {
            if (_client.IsConnected)
                return;

            var clientId = $"{_mqttOptions.ClientId}-subscriber";

            var builder = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer(_mqttOptions.Host, _mqttOptions.Port)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_mqttOptions.KeepAliveSeconds))
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithCleanSession();

            if (!string.IsNullOrWhiteSpace(_mqttOptions.Username))
            {
                builder = builder.WithCredentials(_mqttOptions.Username, _mqttOptions.Password);
            }

            if (_mqttOptions.UseTls)
            {
                builder = builder.WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12
                });
            }

            await _client.ConnectAsync(builder.Build(), cancellationToken);

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(filter =>
                {
                    filter.WithTopic(_subscriberOptions.TopicFilter);
                    filter.WithQualityOfServiceLevel(
                        (MqttQualityOfServiceLevel)Math.Clamp(_subscriberOptions.Qos, 0, 2));
                })
                .Build();

            var result = await _client.SubscribeAsync(subscribeOptions, cancellationToken);
            if (!result.Items.Any(item => (int)item.ResultCode < 128))
                throw new InvalidOperationException("The broker rejected the MQTT QR capture subscription.");

            _logger.LogInformation(
                "MQTT subscriber connected. Host={Host}, Port={Port}, ClientId={ClientId}, TopicFilter={TopicFilter}",
                _mqttOptions.Host,
                _mqttOptions.Port,
                clientId,
                _subscriberOptions.TopicFilter);
        }

        private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            var workflowId = Guid.NewGuid().ToString("N");
            var topic = args.ApplicationMessage.Topic;

            using var logScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["WorkflowId"] = workflowId,
                ["MqttTopic"] = topic ?? string.Empty
            });

            _logger.LogInformation(
                "MQTT EXIT FLOW START. WorkflowId={WorkflowId}, Topic={Topic}",
                workflowId,
                topic);

            if (!TryExtractDeviceId(topic, out var deviceId))
            {
                _logger.LogWarning(
                    "MQTT EXIT FLOW STOPPED. Invalid topic. WorkflowId={WorkflowId}, Topic={Topic}",
                    workflowId,
                    topic);

                return;
            }

            if (args.ApplicationMessage.Retain)
                return;

            var payloadBytes = args.ApplicationMessage.PayloadSegment.ToArray();
            var rawPayloadSpacedHex = ToSpacedHex(payloadBytes);

            _logger.LogInformation(
                "MQTT packet received. WorkflowId={WorkflowId}, DeviceId={DeviceId}, PayloadLength={PayloadLength}, RawPayloadHex={RawPayloadHex}",
                workflowId,
                deviceId,
                payloadBytes.Length,
                rawPayloadSpacedHex);

            using var scope = _scopeFactory.CreateScope();

            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            _logger.LogInformation(
                "Sending ProcessMqttExitPacketCommand. WorkflowId={WorkflowId}, DeviceId={DeviceId}",
                workflowId,
                deviceId);

            var result = await sender.Send(
                new ProcessMqttExitPacketCommand
                {
                    DeviceId = deviceId,
                    PayloadBytes = payloadBytes
                },
                _stoppingToken);

            if (!result.IsSuccess)
            {
                var error = result.Errors.FirstOrDefault();

                _logger.LogError(
                    "ProcessMqttExitPacketCommand failed. WorkflowId={WorkflowId}, DeviceId={DeviceId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    workflowId,
                    deviceId,
                    error?.Code,
                    error?.Message);

                await _notifier.NotifyCaptureDetectedAsync(
                    new MqttCaptureDetectedResponse
                    {
                        DeviceId = deviceId,
                        CaptureId = false,
                        PayloadHex = rawPayloadSpacedHex,
                        WorkflowSucceeded = false,
                        ErrorCode = error?.Code ?? "MqttExit.UnknownError",
                        ErrorMessage = error?.Message ?? "Unknown MQTT exit error."
                    },
                    _stoppingToken);

                return;
            }

            var workflow = result.Value;

            _logger.LogInformation(
                "ProcessMqttExitPacketCommand completed. WorkflowId={WorkflowId}, DeviceId={DeviceId}, IsValidForExit={IsValidForExit}, TicketExited={TicketExited}, GateOpened={GateOpened}, WorkflowSucceeded={WorkflowSucceeded}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                workflowId,
                workflow.DeviceId,
                workflow.IsValidForExit,
                workflow.TicketExited,
                workflow.GateOpened,
                workflow.WorkflowSucceeded,
                workflow.ErrorCode,
                workflow.ErrorMessage);

            await _notifier.NotifyCaptureDetectedAsync(
                new MqttCaptureDetectedResponse
                {
                    DeviceId = workflow.DeviceId,
                    CaptureId = true,
                    PayloadHex = ToSpacedHex(workflow.PayloadHex),
                    ControllerIdFromPacket = workflow.ControllerIdFromPacket,
                    MyDataHex = ToSpacedHex(workflow.MyDataHex),
                    ActualQrHex = ToSpacedHex(workflow.ActualQrHex),
                    QrString = workflow.QrString,
                    TicketId = workflow.TicketId,
                    OperationId = workflow.OperationId,
                    OperationStatus = workflow.OperationStatus,
                    IsValidForExit = workflow.IsValidForExit,
                    TicketExited = workflow.TicketExited,
                    GateOpened = workflow.GateOpened,
                    WorkflowSucceeded = workflow.WorkflowSucceeded,
                    OpenGateTopic = workflow.OpenGateTopic,
                    OpenGatePayload = ToSpacedHex(workflow.OpenGatePayload ?? string.Empty),
                    ErrorCode = workflow.ErrorCode,
                    ErrorMessage = workflow.ErrorMessage
                },
                _stoppingToken);

            _logger.LogInformation(
                "MQTT EXIT FLOW END. WorkflowId={WorkflowId}, DeviceId={DeviceId}, WorkflowSucceeded={WorkflowSucceeded}",
                workflowId,
                workflow.DeviceId,
                workflow.WorkflowSucceeded);
        }

        private static bool TryExtractDeviceId(string? topic, out string deviceId)
        {
            deviceId = string.Empty;

            if (string.IsNullOrWhiteSpace(topic))
                return false;

            var parts = topic.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Expected:
            // publish/network/{deviceId}
            if (parts.Length != 3)
                return false;

            if (!parts[0].Equals("publish", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!parts[1].Equals("network", StringComparison.OrdinalIgnoreCase))
                return false;

            deviceId = parts[2];

            return !string.IsNullOrWhiteSpace(deviceId);
        }

        private static string ToSpacedHex(byte[] bytes)
        {
            if (bytes.Length == 0)
                return string.Empty;

            return string.Join(" ", bytes.Select(b => b.ToString("X2")));
        }

        private static string ToSpacedHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return string.Empty;

            hex = hex
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("{", string.Empty)
                .Replace("}", string.Empty)
                .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase);

            if (hex.Length % 2 != 0)
                return hex;

            return string.Join(
                " ",
                Enumerable.Range(0, hex.Length / 2)
                    .Select(i => hex.Substring(i * 2, 2).ToUpperInvariant()));
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_client.IsConnected)
                    await _client.DisconnectAsync(cancellationToken: cancellationToken);
            }
            catch
            {
                // Best effort shutdown.
            }

            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _client.Dispose();
            base.Dispose();
        }
    }
}