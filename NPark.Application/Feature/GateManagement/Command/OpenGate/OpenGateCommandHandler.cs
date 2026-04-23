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
            var payloadSuffix = "A3AEBEC1B17D";

            var payloadObject = $"{payloadPrefix}{controllerIdHex}{payloadSuffix}";
            await _mqttPublisher.PublishStringAsync(
                topic,
                payloadObject.Trim(),
                new MqttPublishOptions(Qos: 1, Retain: false),
                cancellationToken);

            var response = new OpenGateCommandResponse
            {
                ControllerId = controllerId,
                Topic = topic,
                Payload = payloadObject,
                PublishedAtUtc = DateTime.UtcNow
            };

            return Result<OpenGateCommandResponse>.Ok(response);
        }
    }
}