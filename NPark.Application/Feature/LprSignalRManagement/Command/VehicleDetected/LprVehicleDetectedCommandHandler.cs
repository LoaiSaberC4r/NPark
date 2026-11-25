using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPark.Application.Abstraction;
using NPark.Application.Options;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.LprSignalRManagement.Command.VehicleDetected;

internal sealed class LprVehicleDetectedCommandHandler(
    IUnitOfWork db,
    ILprSignalRNotifier notifier,
    ParkingBusinessClock clock,
    IOptions<LprCaptureOptions> captures,
    IOptions<SubscriberButtonLockOptions> locks,
    GateOperationService operations,
    ILogger<LprVehicleDetectedCommandHandler> logger)
    : ICommandHandler<LprVehicleDetectedCommand, LprVehicleDetectedCommandResponse>
{
    public async Task<Result<LprVehicleDetectedCommandResponse>> Handle(
        LprVehicleDetectedCommand request, CancellationToken ct)
    {
        var receivedAtUtc = clock.UtcNow;
        var lprIp = request.LprIp.Trim();
        var vehicleNumber = request.VehicleNumber.Trim();
        var gate = await db.Repository<ParkingGate>().GetByPropertyAsync(
            x => x.LprIp != null && x.LprIp == lprIp, ct);
        if (gate is null)
            return Result<LprVehicleDetectedCommandResponse>.Fail(
                new Error("LPR_IP_NOT_FOUND", ErrorMessage.LprIpNotFound, ErrorType.NotFound));

        if (captures.Value.Enabled)
        {
            var controller = AuthorizeGateOperationHandler.ParseController(gate.ControlIp);
            var capture = new LprCapture(gate.Id, controller, lprIp, request.VehicleNumber, receivedAtUtc);
            if (await PersistAsync(capture, ct) && gate.GateType == GateType.Entrance && locks.Value.Enabled)
            {
                if (controller is not ushort id || !locks.Value.Supports(id))
                    logger.LogDebug("Subscriber lock unsupported. GateId={GateId}, ControllerId={ControllerId}, LprCaptureId={LprCaptureId}",
                        gate.Id, controller, capture.Id);
                else
                    await TryLockAsync(capture, id, ct);
            }
        }

        var response = new LprVehicleDetectedCommandResponse { GateId = gate.Id, VehicleNumber = vehicleNumber };
        await notifier.NotifyVehicleDetectedAsync(response, ct);
        return Result<LprVehicleDetectedCommandResponse>.Ok(response);
    }

    private async Task<bool> PersistAsync(LprCapture capture, CancellationToken ct)
    {
        try
        {
            await db.Repository<LprCapture>().AddAsync(capture, ct);
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception error) when (!ct.IsCancellationRequested)
        {
            logger.LogError(error, "LPR persistence failed. GateId={GateId}, ControllerId={ControllerId}, " +
                "LprCaptureId={LprCaptureId}, ReceivedAtUtc={ReceivedAtUtc}",
                capture.GateId, capture.ControllerId, capture.Id, capture.CapturedAtUtc);
            return false;
        }
    }

    private async Task TryLockAsync(LprCapture capture, ushort controller, CancellationToken ct)
    {
        try
        {
            var result = await operations.AuthorizeSubscriberLockAsync(capture, controller, ct);
            if (result.IsFailure)
            {
                if (result.Errors.Any(error => error.Code != "SubscriberLockNotEligible"))
                    logger.LogWarning("Subscriber lock declined. GateId={GateId}, ControllerId={ControllerId}, " +
                        "LprCaptureId={LprCaptureId}, ReasonCodes={ReasonCodes}",
                        capture.GateId, controller, capture.Id, string.Join(",", result.Errors.Select(error => error.Code)));
                return;
            }

            var operation = await operations.WaitForSubscriberLockAsync(result.Value, ct);
            if (operation.Status != "Completed")
                logger.LogWarning("Subscriber lock did not complete. GateId={GateId}, ControllerId={ControllerId}, " +
                    "LprCaptureId={LprCaptureId}, GateOperationId={GateOperationId}, CommandId={CommandId}, Status={Status}, ReasonCode={ReasonCode}",
                    capture.GateId, controller, capture.Id, operation.OperationId, operation.CommandId, operation.Status, operation.ReasonCode);
        }
        catch (Exception error) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(error, "Subscriber lock failed. GateId={GateId}, ControllerId={ControllerId}, LprCaptureId={LprCaptureId}",
                capture.GateId, controller, capture.Id);
        }
    }
}
