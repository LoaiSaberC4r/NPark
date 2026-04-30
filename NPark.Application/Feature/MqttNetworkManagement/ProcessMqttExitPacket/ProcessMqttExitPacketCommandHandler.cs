using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Application.Feature.GateManagement.Command.OpenGate;
using NPark.Application.Feature.TicketsManagement.Command.ExitTicketFromMqtt.ExitTicketFromMqttCommand;
using NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText;

namespace NPark.Application.Feature.MqttNetworkManagement.ProcessMqttExitPacket
{
    internal sealed class ProcessMqttExitPacketCommandHandler
         : ICommandHandler<ProcessMqttExitPacketCommand, ProcessMqttExitPacketResponse>
    {
        private readonly IMqttExitQrPacketDecoder _decoder;
        private readonly ISender _sender;
        private readonly ILogger<ProcessMqttExitPacketCommandHandler> _logger;

        public ProcessMqttExitPacketCommandHandler(
            IMqttExitQrPacketDecoder decoder,
            ISender sender,
            ILogger<ProcessMqttExitPacketCommandHandler> logger)
        {
            _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ProcessMqttExitPacketResponse>> Handle(
            ProcessMqttExitPacketCommand request,
            CancellationToken cancellationToken)
        {
            var deviceId = request.DeviceId.Trim();

            _logger.LogInformation(
                "Process MQTT exit started. DeviceId={DeviceId}, PayloadLength={PayloadLength}",
                deviceId,
                request.PayloadBytes?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                _logger.LogWarning("Process MQTT exit stopped. DeviceId is empty.");

                return Fail(
                    "MqttExit.DeviceId.Required",
                    "Device id is required.");
            }

            if (!int.TryParse(deviceId, out var topicControllerId))
            {
                _logger.LogWarning(
                    "Process MQTT exit stopped. DeviceId is not numeric. DeviceId={DeviceId}",
                    deviceId);

                return Fail(
                    "MqttExit.DeviceId.Invalid",
                    "Device id must be numeric.");
            }

            var decode = _decoder.Decode(request.PayloadBytes);

            if (!decode.IsSuccess)
            {
                _logger.LogWarning(
                    "Decode failed. DeviceId={DeviceId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, RawFrameHex={RawFrameHex}",
                    deviceId,
                    decode.ErrorCode,
                    decode.ErrorMessage,
                    ToSpacedHex(decode.RawFrameHex));

                return Result<ProcessMqttExitPacketResponse>.Ok(
                    new ProcessMqttExitPacketResponse
                    {
                        DeviceId = deviceId,
                        PayloadHex = decode.RawFrameHex,
                        ErrorCode = decode.ErrorCode,
                        ErrorMessage = decode.ErrorMessage
                    });
            }

            _logger.LogInformation(
                "Decode success. DeviceId={DeviceId}, HasControllerId={HasControllerId}, ControllerIdFromPacket={ControllerIdFromPacket}, ControllerIdHex={ControllerIdHex}, MyDataHex={MyDataHex}, ActualQrHex={ActualQrHex}, QrString={QrString}",
                deviceId,
                decode.HasControllerId,
                decode.ControllerId,
                ToSpacedHex(decode.ControllerIdHex),
                ToSpacedHex(decode.MyDataHex),
                decode.ActualQrHex,
                decode.QrString);

            if (decode.HasControllerId &&
                decode.ControllerId.HasValue &&
                decode.ControllerId.Value != topicControllerId)
            {
                _logger.LogWarning(
                    "ControllerId mismatch. TopicDeviceId={TopicDeviceId}, PacketControllerId={PacketControllerId}",
                    topicControllerId,
                    decode.ControllerId.Value);

                return Result<ProcessMqttExitPacketResponse>.Ok(
                    new ProcessMqttExitPacketResponse
                    {
                        DeviceId = deviceId,
                        ControllerIdFromPacket = decode.ControllerId,
                        PayloadHex = decode.RawFrameHex,
                        MyDataHex = decode.MyDataHex,
                        ActualQrHex = decode.ActualQrHex,
                        QrString = decode.QrString,
                        ErrorCode = "MqttExit.ControllerId.Mismatch",
                        ErrorMessage = $"Topic deviceId '{deviceId}' does not match packet controllerId '{decode.ControllerId.Value}'."
                    });
            }

            _logger.LogInformation(
                "Sending ValidateExitByTextCommand. DeviceId={DeviceId}, Text={Text}",
                deviceId,
                decode.QrString);

            var validationResult = await _sender.Send(
                new ValidateExitByTextCommand
                {
                    Text = decode.QrString
                },
                cancellationToken);

            if (!validationResult.IsSuccess)
            {
                var error = validationResult.Errors.FirstOrDefault();

                _logger.LogWarning(
                    "ValidateExitByTextCommand failed. DeviceId={DeviceId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    deviceId,
                    error?.Code,
                    error?.Message);

                return FailFrom(
                    validationResult,
                    deviceId,
                    decode);
            }

            var validation = validationResult.Value;

            _logger.LogInformation(
                "ValidateExitByTextCommand completed. DeviceId={DeviceId}, IsValidForExit={IsValidForExit}, TicketId={TicketId}, MatchType={MatchType}, TicketNumber={TicketNumber}, StartDate={StartDate}, EndDate={EndDate}",
                deviceId,
                validation.IsValidForExit,
                validation.TicketId,
                validation.MatchType,
                validation.TicketNumber,
                validation.StartDate,
                validation.EndDate);

            if (!validation.IsValidForExit || validation.TicketId is null)
            {
                _logger.LogWarning(
                    "MQTT exit stopped after validation. DeviceId={DeviceId}, IsValidForExit={IsValidForExit}, TicketId={TicketId}",
                    deviceId,
                    validation.IsValidForExit,
                    validation.TicketId);

                return Result<ProcessMqttExitPacketResponse>.Ok(
                    new ProcessMqttExitPacketResponse
                    {
                        DeviceId = deviceId,
                        ControllerIdFromPacket = decode.ControllerId,
                        PayloadHex = decode.RawFrameHex,
                        MyDataHex = decode.MyDataHex,
                        ActualQrHex = decode.ActualQrHex,
                        QrString = decode.QrString,
                        TicketId = validation.TicketId,
                        IsValidForExit = false,
                        TicketExited = false,
                        GateOpened = false,
                        ErrorCode = "Ticket.NotValidForExit",
                        ErrorMessage = "Ticket is not valid for exit."
                    });
            }

            _logger.LogInformation(
                "Sending ExitTicketFromMqttCommand. DeviceId={DeviceId}, TicketId={TicketId}",
                deviceId,
                validation.TicketId.Value);

            var exitResult = await _sender.Send(
                new ExitTicketFromMqttCommand
                {
                    TicketId = validation.TicketId.Value,
                    ControllerId = deviceId
                },
                cancellationToken);

            if (!exitResult.IsSuccess)
            {
                var error = exitResult.Errors.FirstOrDefault();

                _logger.LogWarning(
                    "ExitTicketFromMqttCommand failed. DeviceId={DeviceId}, TicketId={TicketId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    deviceId,
                    validation.TicketId.Value,
                    error?.Code,
                    error?.Message);

                return FailFrom(
                    exitResult,
                    deviceId,
                    decode,
                    validation.TicketId,
                    isValidForExit: true);
            }

            _logger.LogInformation(
                "ExitTicketFromMqttCommand completed. DeviceId={DeviceId}, TicketId={TicketId}, GateId={GateId}",
                deviceId,
                exitResult.Value.TicketId,
                exitResult.Value.GateId);

            var openGateResult = await _sender.Send(
                new OpenGateCommand
                {
                    ControllerId = deviceId
                },
                cancellationToken);

            if (!openGateResult.IsSuccess)
            {
                var error = openGateResult.Errors.FirstOrDefault();

                _logger.LogWarning(
                    "OpenGateCommand failed. DeviceId={DeviceId}, TicketId={TicketId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    deviceId,
                    validation.TicketId.Value,
                    error?.Code,
                    error?.Message);

                return FailFrom(
                    openGateResult,
                    deviceId,
                    decode,
                    validation.TicketId,
                    isValidForExit: true,
                    ticketExited: true);
            }

            var openGate = openGateResult.Value;

            _logger.LogInformation(
                "OpenGateCommand completed. DeviceId={DeviceId}, Topic={Topic}, Payload={Payload}, PublishedAtUtc={PublishedAtUtc}",
                deviceId,
                openGate.Topic,
                ToSpacedHex(openGate.Payload),
                openGate.PublishedAtUtc);

            return Result<ProcessMqttExitPacketResponse>.Ok(
                new ProcessMqttExitPacketResponse
                {
                    DeviceId = deviceId,
                    ControllerIdFromPacket = decode.ControllerId,
                    PayloadHex = decode.RawFrameHex,
                    MyDataHex = decode.MyDataHex,
                    ActualQrHex = decode.ActualQrHex,
                    QrString = decode.QrString,
                    TicketId = validation.TicketId,
                    IsValidForExit = true,
                    TicketExited = true,
                    GateOpened = true,
                    OpenGateTopic = openGate.Topic,
                    OpenGatePayload = openGate.Payload
                });
        }

        private static Result<ProcessMqttExitPacketResponse> Fail(
            string code,
            string message)
        {
            return Result<ProcessMqttExitPacketResponse>.Fail(
                new Error(
                    Code: code,
                    Message: message,
                    Type: ErrorType.Validation));
        }

        private static Result<ProcessMqttExitPacketResponse> FailFrom<T>(
            Result<T> result,
            string deviceId,
            MqttExitQrPacketDecodeResult decode,
            Guid? ticketId = null,
            bool isValidForExit = false,
            bool ticketExited = false)
        {
            var error = result.Errors.FirstOrDefault()
                ?? new Error(
                    Code: "MqttExit.UnknownError",
                    Message: "Unknown MQTT exit error.",
                    Type: ErrorType.Infrastructure);

            return Result<ProcessMqttExitPacketResponse>.Ok(
                new ProcessMqttExitPacketResponse
                {
                    DeviceId = deviceId,
                    ControllerIdFromPacket = decode.ControllerId,
                    PayloadHex = decode.RawFrameHex,
                    MyDataHex = decode.MyDataHex,
                    ActualQrHex = decode.ActualQrHex,
                    QrString = decode.QrString,
                    TicketId = ticketId,
                    IsValidForExit = isValidForExit,
                    TicketExited = ticketExited,
                    GateOpened = false,
                    ErrorCode = error.Code,
                    ErrorMessage = error.Message
                });
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