using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Application.Shared.Dto;

namespace NPark.Application.Services;

public sealed class GateOperationNotifier(IServiceScopeFactory scopes, ILogger<GateOperationNotifier> logger)
{
    public async Task NotifyAsync(GateOperationResponse operation, TicketExitedNotification? exit)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var realtime = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
            var gateStatus = realtime.NotifyGateOperationAsync(operation);
            var ticketStatus = exit is null ? Task.CompletedTask : realtime.NotifyTicketExitedAsync(exit);
            await Task.WhenAll(gateStatus, ticketStatus).WaitAsync(TimeSpan.FromSeconds(1));
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Post-commit notification failed for gate operation {OperationId}", operation.OperationId);
        }
    }
}
