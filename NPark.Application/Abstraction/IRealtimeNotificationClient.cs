using NPark.Application.Services;
using NPark.Application.Shared.Dto;

namespace NPark.Application.Abstraction
{
    public interface IRealtimeNotificationClient
    {
        Task GateOperationStatusChanged(GateOperationResponse payload);

        Task TicketAdded(TicketAddedNotification payload);

        Task TicketExited(TicketExitedNotification payload);
    }
}