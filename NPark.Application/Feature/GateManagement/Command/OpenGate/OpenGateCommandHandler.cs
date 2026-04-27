using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using NPark.Application.Abstraction;

namespace NPark.Application.Feature.GateManagement.Command.OpenGate
{
    internal sealed class OpenGateCommandHandler
        : ICommandHandler<OpenGateCommand, OpenGateCommandResponse>
    {
        private readonly IMqttPublisher _mqttPublisher;

        public OpenGateCommandHandler(IMqttPublisher mqttPublisher)
        {
            _mqttPublisher = mqttPublisher;
        }

        public async Task<Result<OpenGateCommandResponse>> Handle(
            OpenGateCommand request,
            CancellationToken cancellationToken)
        {
            var controllerId = request.ControllerId.Trim();
            var topic = $"sub/network/{controllerId}";

            var controllerIdValue = int.Parse(controllerId);
            var controllerIdHex = controllerIdValue.ToString("X4");

            var payloadPrefix = "7B";
            var payloadSuffix = "AEBEC1B17D";

            // الـ payload كامل كـ string hex
            var payloadHexString = $"{payloadPrefix}{controllerIdHex}{payloadSuffix}";

            // تحويل الـ hex string لـ byte array
            var payloadBytes = HexStringToBytes(payloadHexString);

            // نشر الـ payload كـ bytes
            await _mqttPublisher.PublishBytesAsync(
                topic,
                payloadBytes,
                new MqttPublishOptions(Qos: 1, Retain: false),
                cancellationToken);

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
            hex = hex.Replace(" ", "").Replace("-", "");

            if (hex.Length % 2 != 0)
                hex = "0" + hex; // إضافة صفر إذا الطول فردي

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}