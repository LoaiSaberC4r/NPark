using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPark.Application.Abstraction;
using NPark.Domain.Entities;
using NPark.Infrastructure.Options;

namespace NPark.Infrastructure.Services.Mqtt
{
    internal sealed class MqttNetworkHeartbeatService : BackgroundService
    {
        private readonly IMqttPublisher _mqttPublisher;
        private readonly ILogger<MqttNetworkHeartbeatService> _logger;
        private readonly MqttHeartbeatOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;

        public MqttNetworkHeartbeatService(
            IMqttPublisher mqttPublisher,
            IOptions<MqttHeartbeatOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<MqttNetworkHeartbeatService> logger)
        {
            _mqttPublisher = mqttPublisher;
            _logger = logger;
            _options = options.Value;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("MQTT heartbeat background service is disabled.");
                return;
            }

            if (_options.IntervalSeconds <= 0)
            {
                _logger.LogWarning(
                    "MQTT heartbeat background service is not started because IntervalSeconds={IntervalSeconds} is invalid.",
                    _options.IntervalSeconds);
                return;
            }

            var payload = NormalizePayload(_options.Payload);

            _logger.LogInformation(
                "MQTT heartbeat background service started. IntervalSeconds={IntervalSeconds}, Payload={Payload}",
                _options.IntervalSeconds,
                payload);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<NParkDBContext>();

                        var controlIps = await dbContext.Set<ParkingGate>()
                            .AsNoTracking()
                            .Where(x => !string.IsNullOrWhiteSpace(x.ControlIp))
                            .Select(x => x.ControlIp!)
                            .ToListAsync(stoppingToken);

                        foreach (var controlIp in controlIps)
                        {
                            var topic = BuildTopic(_options.TopicPrefix, controlIp);

                            await _mqttPublisher.PublishStringAsync(
                                topic,
                                payload,
                                new MqttPublishOptions(_options.Qos, _options.Retain),
                                stoppingToken);

                            _logger.LogInformation(
                                "MQTT heartbeat published successfully. Topic={Topic}, Payload={Payload}",
                                topic,
                                payload);
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish MQTT heartbeat.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }

            _logger.LogInformation("MQTT heartbeat background service stopped.");
        }

        private static string BuildTopic(string topicPrefix, string deviceId)
        {
            var normalizedPrefix = (topicPrefix ?? string.Empty).Trim().TrimEnd('/');
            var normalizedDeviceId = (deviceId ?? string.Empty).Trim();

            return $"{normalizedPrefix}/{normalizedDeviceId}";
        }

        private static string NormalizePayload(string payload)
        {
            return (payload ?? string.Empty).Trim();
        }
    }
}