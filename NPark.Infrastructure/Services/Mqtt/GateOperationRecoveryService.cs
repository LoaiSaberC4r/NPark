using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
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
using NPark.Infrastructure.Options;

namespace NPark.Infrastructure.Services.Mqtt;

public interface IGateOperationDispatcher
{
    Task<bool> RetryAsync(Guid operationId, Guid gateId, CancellationToken ct);
    Task PublishStatusAsync(GateTransitionResult change);
}

internal sealed class GateOperationRecoveryService(IServiceScopeFactory scopes, IMqttPublisher publisher,
    GateAckSubscriptionState ackSubscription, IOptions<GateOperationOptions> options, IOptions<MqttOptions> brokerOptions,
    IOptions<SubscriberButtonLockOptions> locks, IOptions<AutomaticVisitorEntryOptions> visitors,
    TimeProvider time, GateOperationNotifier notifications, ILogger<GateOperationRecoveryService> logger)
    : BackgroundService, IGateOperationDispatcher
{
    private bool publisherUnavailable;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.WhenAll(
        RunLoopAsync("expiry", ExpireAsync, stoppingToken),
        RunLoopAsync("dispatch", DispatchPendingAsync, stoppingToken),
        RunLoopAsync("completion", CompleteAcknowledgedAsync, stoppingToken));

    private async Task RunLoopAsync(string name, Func<CancellationToken, Task> work, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.RecoveryIntervalSeconds));
        do
        {
            try
            {
                await work(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                logger.LogError(error, "Gate operation {Phase} pass failed", name);
            }
        } while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct));
    }

    private async Task ExpireAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
        var now = time.GetUtcNow().UtcDateTime;
        var ackCutoff = now.AddSeconds(-options.Value.AckTimeoutSeconds);
        var dispatchCutoff = now.AddSeconds(-options.Value.DispatchWaitTimeoutSeconds);
        var enableExitGateMqtt = options.Value.EnableExitGateMqtt;
        var candidates = await db.Set<GateOperation>().AsNoTracking()
            .Where(op => op.CompletionMode == GateOperationCompletionMode.Hardware)
            .Where(op => op.Status == GateOperationStatus.Pending &&
                    (op.DispatchStartedAtUtc == null && (op.CreatedAtUtc <= dispatchCutoff ||
                        (!enableExitGateMqtt && op.Type == GateOperationType.Exit)) || op.DispatchStartedAtUtc <= ackCutoff) ||
                op.Status == GateOperationStatus.Published && op.PublishedAtUtc <= ackCutoff)
            .Select(op => new { op.Id, op.Status })
            .ToListAsync(ct);

        await ProcessBatchAsync(candidates, async (operation, token) =>
        {
            var change = operation.Status == GateOperationStatus.Pending
                ? await CheckPendingTimeoutAsync(operation.Id, token)
                : await AdvanceAsync(operation.Id, GateTransition.CheckTimeout, null, token);
            if (change is not null)
                await PublishStatusAsync(change);
        }, ct);
    }

    private async Task<GateTransitionResult?> CheckPendingTimeoutAsync(Guid id, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        return await TryLockDispatchAsync(db, id, ct)
            ? await AdvanceAsync(id, GateTransition.CheckTimeout, null, ct) : null;
    }

    private async Task DispatchPendingAsync(CancellationToken ct)
    {
        if (!ackSubscription.Ready)
            return;

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
        var cutoff = time.GetUtcNow().UtcDateTime.AddSeconds(-options.Value.DispatchWaitTimeoutSeconds);
        var enableExitGateMqtt = options.Value.EnableExitGateMqtt;
        var lockControllers = locks.Value.Enabled ? locks.Value.SupportedControllerIds : [];
        var visitorControllers = visitors.Value.Enabled ? visitors.Value.SupportedControllerIds : [];
        var ids = await db.Set<GateOperation>().AsNoTracking()
            .Where(op => op.CompletionMode == GateOperationCompletionMode.Hardware &&
                (enableExitGateMqtt || op.Type != GateOperationType.Exit))
            .Where(op => op.Status == GateOperationStatus.Pending && op.DispatchStartedAtUtc == null && op.CreatedAtUtc > cutoff)
            .Where(op => (op.Type != GateOperationType.LockCardButton || lockControllers.Contains(op.ControllerId)) &&
                (!op.IsAutomaticVisitorEntry || visitorControllers.Contains(op.ControllerId)))
            .OrderBy(op => op.CreatedAtUtc).Select(op => op.Id).ToListAsync(ct);
        if (ids.Count == 0)
            return;

        // A connection failure before claiming leaves the original dispatch window in effect.
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(options.Value.AckTimeoutSeconds), time);
        using var connection = CancellationTokenSource.CreateLinkedTokenSource(budget.Token, ct);
        try
        {
            await publisher.EnsureConnectedAsync(connection.Token);
            publisherUnavailable = false;
        }
        catch (Exception error) when (!ct.IsCancellationRequested)
        {
            if (!publisherUnavailable)
                logger.LogWarning(error, "Gate dispatch deferred because the MQTT publisher is unavailable");
            publisherUnavailable = true;
            return;
        }

        await ProcessBatchAsync(ids, async (id, token) =>
        {
            if (await DispatchAsync(id, token) is { } change)
                await PublishStatusAsync(change);
        }, ct);
    }

    private async Task<GateTransitionResult?> DispatchAsync(Guid id, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await TryLockDispatchAsync(db, id, ct) || !ackSubscription.Ready)
            return null;

        var claim = await AdvanceAsync(id, GateTransition.Dispatch, null, ct);
        if (claim?.Operation is not { } operation || claim.DispatchAttemptDeadlineUtc is not DateTime deadline)
            return claim;

        var remaining = deadline - time.GetUtcNow().UtcDateTime;
        if (remaining <= TimeSpan.Zero)
            return await AdvanceAsync(id, GateTransition.DeliveryUnknown, null, CancellationToken.None);

        using var budget = new CancellationTokenSource(remaining, time);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct, budget.Token);
        var sendBy = operation.CreatedAtUtc.AddSeconds(options.Value.DispatchWaitTimeoutSeconds);
        try
        {
            var raw = Convert.FromHexString(operation.Payload);
            logger.LogInformation("Publishing gate command. OperationType={OperationType}, OperationId={OperationId}, CommandId={CommandId}, " +
                "ControllerId={ControllerId}, Topic={Topic}, PayloadHex={PayloadHex}, PayloadLength={PayloadLength}, " +
                "BrokerHost={BrokerHost}, BrokerPort={BrokerPort}, Qos={Qos}, Retain={Retain}",
                operation.OperationType, id, operation.CommandId, operation.ControllerId, operation.Topic, operation.Payload,
                raw.Length, brokerOptions.Value.Host, brokerOptions.Value.Port, options.Value.CommandQos, false);
            await publisher.PublishBytesAsync(operation.Topic, raw,
                new MqttPublishOptions(options.Value.CommandQos, false), timeout.Token,
                () => ackSubscription.Ready && time.GetUtcNow().UtcDateTime < (deadline < sendBy ? deadline : sendBy));
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "MQTT publish failed. OperationType={OperationType}, OperationId={OperationId}, CommandId={CommandId}",
                operation.OperationType, id, operation.CommandId);
            return await AdvanceAsync(id, GateTransition.DeliveryUnknown, null, CancellationToken.None);
        }

        logger.LogInformation("MQTT broker accepted gate command. OperationType={OperationType}, OperationId={OperationId}, CommandId={CommandId}, " +
            "ControllerId={ControllerId}, Topic={Topic}", operation.OperationType, id, operation.CommandId, operation.ControllerId, operation.Topic);
        return await AdvanceAsync(id, GateTransition.Published, null, CancellationToken.None);
    }

    // The outer SQL transaction owns only this application lock. The claim and final state
    // commit separately. Another process may expire a claim only after the publisher settles
    // or its owning SQL session closes. A recovered claim is never sent again.
    private static async Task<bool> TryLockDispatchAsync(NParkDBContext db, Guid id, CancellationToken ct)
    {
        var result = new SqlParameter("result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var resource = new SqlParameter("resource", $"NPark:GateDispatch:{id:N}");
        await db.Database.ExecuteSqlRawAsync(
            "EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockTimeout = 0;",
            new object[] { result, resource }, ct);
        return (int)result.Value switch
        {
            >= 0 => true,
            -1 => false,
            var code => throw new InvalidOperationException($"SQL gate dispatch lock returned {code}.")
        };
    }

    private async Task CompleteAcknowledgedAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
        var ids = await db.Set<GateOperation>().AsNoTracking().Where(op =>
                op.CompletionMode == GateOperationCompletionMode.Hardware && op.Status == GateOperationStatus.Acknowledged)
            .OrderBy(op => op.CreatedAtUtc).Select(op => op.Id).ToListAsync(ct);
        await ProcessBatchAsync(ids, async (id, token) => await RetryAsync(id, null, token), ct);
    }

    public Task<bool> RetryAsync(Guid operationId, Guid gateId, CancellationToken ct) => RetryAsync(operationId, (Guid?)gateId, ct);

    private async Task<bool> RetryAsync(Guid id, Guid? gateId, CancellationToken ct)
    {
        var change = await AdvanceAsync(id, GateTransition.Complete, gateId, ct);
        if (change is null)
        {
            if (gateId.HasValue)
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
                var status = await db.Set<GateOperation>().AsNoTracking().Where(x => x.Id == id && x.GateId == gateId &&
                    (x.Status == GateOperationStatus.Completed || x.Status == GateOperationStatus.Failed ||
                     x.Status == GateOperationStatus.TimedOut || x.Status == GateOperationStatus.Unknown))
                    .Select(x => new { x.Status, x.CommandId, x.ControllerId }).SingleOrDefaultAsync(ct);
                if (status is not null)
                    logger.LogInformation("Terminal gate operation retransmission rejected. OperationId={OperationId}, " +
                        "CommandId={CommandId}, ControllerId={ControllerId}, GateId={GateId}, Status={Status}, ReasonCode={ReasonCode}",
                        id, status.CommandId, status.ControllerId, gateId, status.Status, "GateFreshRequestRequired");
            }
            return false;
        }
        await PublishStatusAsync(change);
        return true;
    }

    private Task ProcessBatchAsync<T>(IEnumerable<T> candidates, Func<T, CancellationToken, Task> process, CancellationToken ct) =>
        Parallel.ForEachAsync(candidates, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct },
            async (candidate, token) =>
            {
                try
                {
                    await process(candidate, token);
                }
                catch (Exception error) when (!token.IsCancellationRequested)
                {
                    logger.LogWarning(error, "Gate recovery could not process {Candidate}; the next pass will read current state", candidate);
                }
            });

    private async Task<GateTransitionResult?> AdvanceAsync(Guid id, GateTransition transition, Guid? gateId, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var command = new AdvanceGateOperation(id, transition, gateId);
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(command, ct);
        if (result.IsSuccess)
            return result.Value.Operation is null ? null : result.Value;

        logger.LogWarning("Gate transition {Transition} failed for {OperationId}: {Codes}",
            transition, id, string.Join(",", result.Errors.Select(x => x.Code)));
        return null;
    }

    public async Task PublishStatusAsync(GateTransitionResult change)
    {
        if (change.Operation is not { } operation)
            return;
        var message = operation.Status switch
        {
            "TimedOut" when change.PreviousStatus != GateOperationStatus.TimedOut => "Gate operation timed out",
            "Unknown" when change.PreviousStatus != GateOperationStatus.Unknown => "Gate delivery outcome unknown",
            "Acknowledged" => "Successful hardware ACK persisted; gate completion pending",
            "Completed" when operation.TicketId.HasValue => "Gate operation and ticket completed",
            _ => "Gate operation updated"
        };
        logger.LogInformation("{Outcome}. OperationId={OperationId}, CommandId={CommandId}, ControllerId={ControllerId}, " +
            "TicketId={TicketId}, GateId={GateId}, OperationType={OperationType}, PreviousStatus={PreviousStatus}, " +
            "NewStatus={NewStatus}, ReasonCode={ReasonCode}, ControllerReservationReleased={ControllerReservationReleased}, " +
            "TicketReservationReleased={TicketReservationReleased}, AckStatusCode={AckStatusCode}",
            message, operation.OperationId, operation.CommandId, operation.ControllerId, operation.TicketId, operation.GateId,
            operation.OperationType, change.PreviousStatus, operation.Status, operation.ReasonCode, change.ControllerReleased,
            change.TicketReleased, operation.AckStatusCode);
        await notifications.NotifyAsync(operation, change.Exit);
    }
}
