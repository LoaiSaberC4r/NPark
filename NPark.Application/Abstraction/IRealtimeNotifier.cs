using NPark.Application.Services;
using NPark.Application.Shared.Dto;

namespace NPark.Application.Abstraction
{
    public interface IRealtimeNotifier
    {
        Task NotifyGateOperationAsync(GateOperationResponse payload);

        Task NotifyTicketAddedAsync(TicketAddedNotification payload);

        Task NotifyTicketExitedAsync(TicketExitedNotification payload);
    }
}