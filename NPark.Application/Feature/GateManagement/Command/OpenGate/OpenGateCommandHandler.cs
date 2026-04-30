using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;

namespace NPark.Application.Feature.GateManagement.Command.OpenGate
{
    internal sealed class OpenGateCommandHandler
        : ICommandHandler<OpenGateCommand, OpenGateCommandResponse>
    {
        private readonly IMqttPublisher _mqttPublisher;
        private readonly ILogger<OpenGateCommandHandler> _logger;

        public OpenGateCommandHandler(
            IMqttPublisher mqttPublisher,
            ILogger<OpenGateCommandHandler> logger)
        {
            _mqttPublisher = mqttPublisher
                ?? throw new ArgumentNullException(nameof(mqttPublisher));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<OpenGateCommandResponse>> Handle(
            OpenGateCommand request,
            CancellationToken cancellationToken)
        {
            var controllerId = request.ControllerId.Trim();

            _logger.LogInformation(
                "OpenGate started. ControllerId={ControllerId}",
                controllerId);

            if (!int.TryParse(controllerId, out var controllerIdValue))
            {
                _logger.LogWarning(
                    "OpenGate stopped. ControllerId is invalid. ControllerId={ControllerId}",
                    controllerId);

                return Result<OpenGateCommandResponse>.Fail(
                    new Error(
                        Code: "OpenGate.ControllerId.Invalid",
                        Message: "Controller id must be numeric.",
                        Type: ErrorType.Validation));
            }

            var topic = $"sub/network/{controllerId}";

            var controllerIdHex = controllerIdValue.ToString("X4");

            const string payloadPrefix = "7B";
            const string payloadSuffix = "AEBEC1B17D";

            var payloadHexString = $"{payloadPrefix}{controllerIdHex}{payloadSuffix}";

            _logger.LogInformation(
                "OpenGate payload built. ControllerId={ControllerId}, ControllerIdHex={ControllerIdHex}, Topic={Topic}, PayloadHex={PayloadHex}",
                controllerId,
                controllerIdHex,
                topic,
                ToSpacedHex(payloadHexString));

            var payloadBytes = HexStringToBytes(payloadHexString);

            _logger.LogInformation(
                "Publishing OpenGate MQTT message. Topic={Topic}, PayloadBytesLength={PayloadBytesLength}, PayloadHex={PayloadHex}",
                topic,
                payloadBytes.Length,
                ToSpacedHex(payloadHexString));

            await _mqttPublisher.PublishBytesAsync(
                topic,
                payloadBytes,
                new MqttPublishOptions(Qos: 1, Retain: false),
                cancellationToken);

            _logger.LogInformation(
                "OpenGate MQTT message published successfully. Topic={Topic}, PayloadHex={PayloadHex}",
                topic,
                ToSpacedHex(payloadHexString));

            var response = new OpenGateCommandResponse
            {
                ControllerId = controllerId,
                Topic = topic,
                Payload = payloadHexString,
                PublishedAtUtc = DateTime.UtcNow
            };

            return Result<OpenGateCommandResponse>.Ok(response);
        }

        private static ReadOnlyMemory<byte> HexStringToBytes(string hex)
        {
            hex = hex
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("{", string.Empty)
                .Replace("}", string.Empty)
                .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase);

            if (hex.Length % 2 != 0)
                hex = "0" + hex;

            var bytes = new byte[hex.Length / 2];

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
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
    }
}