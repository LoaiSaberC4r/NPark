using BuildingBlock.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPark.Application.Abstraction;
using NPark.Application.Options;
using NPark.Application.Services;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Infrastructure.Services.Mqtt;

public interface IGateOperationDispatcher
{
    Task<bool> RetryAsync(Guid operationId, Guid gateId, CancellationToken ct);
    Task PublishStatusAsync(GateTransitionResult change);
}

internal sealed class GateOperationRecoveryService(IServiceScopeFactory scopes, IMqttPublisher publisher,
    GateAckSubscriptionState ackSubscription, IOptions<GateOperationOptions> options,
    TimeProvider time, ILogger<GateOperationRecoveryService> logger)
    : BackgroundService, IGateOperationDispatcher
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.RecoveryIntervalSeconds));
        do
        {
            try
            {
                await RecoverAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                logger.LogError(error, "The gate operation recovery pass failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RecoverAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
        var configuration = options.Value;
        var ready = ackSubscription.Ready;
        var utcNow = time.GetUtcNow().UtcDateTime;
        var timeout = utcNow.AddSeconds(-configuration.AckTimeoutSeconds);
        var retryBefore = timeout.AddMilliseconds(-configuration.RetryIntervalMilliseconds);
        var autoRetry = ready && configuration.EnableAutomaticRetry && configuration.HardwareDeduplicationConfirmed;
        var candidates = await db.Set<GateOperation>().AsNoTracking()
            .Where(op => op.Status == GateOperationStatus.Acknowledged ||
                op.Status == GateOperationStatus.Pending &&
                    (op.DispatchStartedAtUtc == null && ready || op.DispatchStartedAtUtc <= timeout) ||
                op.Status == GateOperationStatus.Published && op.PublishedAtUtc <= timeout ||
                autoRetry && op.RetryCount < configuration.MaxRetries && op.DispatchStartedAtUtc <= retryBefore &&
                    (op.Status == GateOperationStatus.Unknown || op.Status == GateOperationStatus.TimedOut))
            .OrderBy(op => op.Status == GateOperationStatus.Acknowledged ? 0 : 1)
            .ThenBy(op => op.CreatedAtUtc)
            .Select(op => new { op.Id, op.Status, op.DispatchStartedAtUtc })
            .ToListAsync(ct);

        await Parallel.ForEachAsync(candidates, new ParallelOptions
        {
            MaxDegreeOfParallelism = 8,
            CancellationToken = ct
        }, async (op, cancellationToken) =>
        {
            var transition = op.Status switch
            {
                GateOperationStatus.Acknowledged => GateTransition.Complete,
                GateOperationStatus.Pending when op.DispatchStartedAtUtc is null => GateTransition.Dispatch,
                GateOperationStatus.Pending or GateOperationStatus.Published => GateTransition.CheckTimeout,
                _ => GateTransition.Retry
            };
            if (transition is GateTransition.Dispatch or GateTransition.Retry)
                await DispatchAsync(op.Id, transition, null, cancellationToken);
            else
                await AdvanceAsync(op.Id, transition, null, cancellationToken);
        });
    }

    public async Task<bool> RetryAsync(Guid operationId, Guid gateId, CancellationToken ct)
    {
        var completed = await AdvanceAsync(operationId, GateTransition.Complete, gateId, ct);
        return completed != null || await DispatchAsync(operationId, GateTransition.Retry, gateId, ct);
    }

    private async Task<bool> DispatchAsync(Guid operationId, GateTransition transition, Guid? gateId, CancellationToken ct)
    {
        if (!ackSubscription.Ready)
            return false;
        var change = await AdvanceAsync(operationId, transition, gateId, ct, notify: false);
        if (change?.Operation is not { } operation)
            return false;

        await using var check = scopes.CreateAsyncScope();
        var db = check.ServiceProvider.GetRequiredService<NParkDBContext>();
        if (!await db.Set<GateOperation>().AnyAsync(x => x.Id == operationId && x.Status == GateOperationStatus.Pending, ct))
            return true;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.AckTimeoutSeconds));
        try
        {
            await publisher.PublishBytesAsync(operation.Topic, Convert.FromHexString(operation.Payload),
                new MqttPublishOptions(options.Value.CommandQos, false), timeout.Token);
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "MQTT dispatch outcome unknown for {OperationId}, command {CommandId}",
                operation.OperationId, operation.CommandId);
            await AdvanceAsync(operationId, GateTransition.DeliveryUnknown, gateId, CancellationToken.None);
            return true;
        }

        await AdvanceAsync(operationId, GateTransition.Published, gateId, CancellationToken.None);
        return true;
    }

    private async Task<GateTransitionResult?> AdvanceAsync(Guid id, GateTransition transition, Guid? gateId,
        CancellationToken ct, bool notify = true)
    {
        await using var scope = scopes.CreateAsyncScope();
        var command = new AdvanceGateOperation(id, transition, gateId);
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(command, ct);
        if (result.IsFailure)
        {
            logger.LogWarning("Gate transition {Transition} failed for {OperationId}: {Codes}",
                transition, id, string.Join(",", result.Errors.Select(x => x.Code)));
            return null;
        }
        if (result.Value.Operation is null)
            return null;
        if (notify)
            await PublishStatusAsync(result.Value);
        return result.Value;
    }

    public async Task PublishStatusAsync(GateTransitionResult change)
    {
        if (change.Operation is not { } operation)
            return;
        logger.LogInformation(
            "Gate {OperationType} {OperationId} command {CommandId} controller {ControllerId} ticket {TicketId} at gate {GateId} is {Status}, ACK {Ack}",
            operation.OperationType, operation.OperationId, operation.CommandId, operation.ControllerId,
            operation.TicketId, operation.GateId, operation.Status, operation.AckStatusCode);
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var realtime = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
            if (change.Exit != null)
                await realtime.NotifyTicketExitedAsync(change.Exit);
            await realtime.NotifyGateOperationAsync(operation);
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Post-commit notification failed for gate operation {OperationId}", operation.OperationId);
        }
    }
}
