using NPark.Domain.Resource;

namespace NPark.Application.Services;

internal static class WorkflowMessages
{
    public static string Get(string code) => code switch
    {
        "TicketNotFound" => ErrorMessage.Ticket_NotFound,
        "GateNotFound" => ErrorMessage.GateNotFound,
        _ => ErrorMessage.ResourceManager.GetString($"ParkingWorkflow_{code}") ?? code
    };
}
