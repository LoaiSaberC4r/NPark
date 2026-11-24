using System.Globalization;
using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using MediatR;
using NPark.Application.Abstraction;
using NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText;
using NPark.Application.Services;
using NPark.Domain.Enums;

namespace NPark.Application.Feature.MqttNetworkManagement.ProcessMqttExitPacket;

internal sealed class ProcessMqttExitPacketCommandHandler(IMqttExitQrPacketDecoder decoder, ISender sender,
    GateOperationService operations) : ICommandHandler<ProcessMqttExitPacketCommand, ProcessMqttExitPacketResponse>
{
    public async Task<Result<ProcessMqttExitPacketResponse>> Handle(ProcessMqttExitPacketCommand request, CancellationToken ct)
    {
        if (!ushort.TryParse(request.DeviceId, NumberStyles.None, CultureInfo.InvariantCulture, out var controller) || controller == 0)
            return Result<ProcessMqttExitPacketResponse>.Fail(new Error("MqttExit.DeviceId.Invalid",
                "The controller identifier must be between 1 and 65535.", ErrorType.Validation));

        var packet = decoder.Decode(request.PayloadBytes);
        var response = new ProcessMqttExitPacketResponse
        {
            DeviceId = request.DeviceId,
            ControllerIdFromPacket = packet.ControllerId,
            PayloadHex = packet.RawFrameHex,
            MyDataHex = packet.MyDataHex,
            ActualQrHex = packet.ActualQrHex,
            QrString = packet.QrString,
            ErrorCode = packet.ErrorCode,
            ErrorMessage = packet.ErrorMessage
        };
        if (!packet.IsSuccess)
            return Result<ProcessMqttExitPacketResponse>.Ok(response);
        if (packet.HasControllerId && packet.ControllerId != controller)
            return Result<ProcessMqttExitPacketResponse>.Ok(response with
            {
                ErrorCode = "MqttExit.ControllerId.Mismatch",
                ErrorMessage = "The packet and topic controller identifiers differ."
            });

        var lookup = await sender.Send(new ValidateExitByTextCommand { Text = packet.QrString, QrOnly = true }, ct);
        if (lookup.IsFailure)
            return Result<ProcessMqttExitPacketResponse>.Fail(lookup.Errors);
        if (lookup.Value.TicketId is not Guid ticketId)
            return Result<ProcessMqttExitPacketResponse>.Ok(response with { ErrorCode = lookup.Value.ReasonCode });

        var authorized = await operations.AuthorizeAsync(ticketId, null, controller, null, GateOperationType.Exit, ct);
        if (authorized.IsFailure)
        {
            var error = authorized.Errors.First();
            return Result<ProcessMqttExitPacketResponse>.Ok(response with
                { TicketId = ticketId, ErrorCode = error.Code, ErrorMessage = error.Message });
        }
        var operation = authorized.Value;
        return Result<ProcessMqttExitPacketResponse>.Ok(response with
        {
            TicketId = ticketId,
            OperationId = operation.OperationId,
            OperationStatus = operation.Status,
            IsValidForExit = true,
            TicketExited = operation.TicketExited,
            GateOpened = operation.IsAcknowledged,
            OpenGateTopic = operation.Topic,
            OpenGatePayload = operation.Payload
        });
    }
}
