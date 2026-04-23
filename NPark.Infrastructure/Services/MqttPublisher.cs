using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using NPark.Application.Abstraction;
using NPark.Infrastructure.Options;
using System.Security.Authentication;
using System.Text;

namespace NPark.Infrastructure.Services
{
    internal sealed class MqttPublisher : IMqttPublisher, IAsyncDisposable
    {
        private readonly ILogger<MqttPublisher> _logger;
        private readonly MqttOptions _options;
        private readonly IMqttClient _client;
        private readonly SemaphoreSlim _connectLock = new(1, 1);

        public MqttPublisher(
            IOptions<MqttOptions> options,
            ILogger<MqttPublisher> logger)
        {
            _logger = logger;
            _options = options.Value;
            _client = new MqttFactory().CreateMqttClient();
        }

        public Task PublishStringAsync(
            string topic,
            string payload,
            MqttPublishOptions options,
            CancellationToken ct)
            => PublishBytesAsync(topic, Encoding.UTF8.GetBytes(payload), options, ct);

        public async Task PublishBytesAsync(
            string topic,
            ReadOnlyMemory<byte> payload,
            MqttPublishOptions options,
            CancellationToken ct)
        {
            await EnsureConnectedAsync(ct);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload.ToArray())
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)Math.Clamp(options.Qos, 0, 2))
                .WithRetainFlag(options.Retain)
                .Build();

            await _client.PublishAsync(message, ct);

            _logger.LogInformation(
                "MQTT message published. Topic={Topic}, Qos={Qos}, Retain={Retain}, PayloadSize={PayloadSize}",
                topic,
                options.Qos,
                options.Retain,
                payload.Length);
        }

        private async Task EnsureConnectedAsync(CancellationToken ct)
        {
            if (_client.IsConnected)
                return;

            await _connectLock.WaitAsync(ct);
            try
            {
                if (_client.IsConnected)
                    return;

                var builder = new MqttClientOptionsBuilder()
                    .WithClientId(_options.ClientId)
                    .WithTcpServer(_options.Host, _options.Port)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(_options.KeepAliveSeconds))
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .WithCleanSession();

                if (!string.IsNullOrWhiteSpace(_options.Username))
                {
                    builder = builder.WithCredentials(_options.Username, _options.Password);
                }

                if (_options.UseTls)
                {
                    builder = builder.WithTls(new MqttClientOptionsBuilderTlsParameters
                    {
                        UseTls = true,
                        SslProtocol = SslProtocols.Tls12
                    });
                }

                await _client.ConnectAsync(builder.Build(), ct);

                _logger.LogInformation(
                    "MQTT connected successfully. Host={Host}, Port={Port}, UseTls={UseTls}",
                    _options.Host,
                    _options.Port,
                    _options.UseTls);
            }
            finally
            {
                _connectLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_client.IsConnected)
                    await _client.DisconnectAsync();
            }
            catch
            {
            }

            _client.Dispose();
            _connectLock.Dispose();
        }
    }
}