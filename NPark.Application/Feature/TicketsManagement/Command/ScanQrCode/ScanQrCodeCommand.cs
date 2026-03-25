using BuildingBlock.Application.Abstraction;
using NPark.Application.Feature.TicketsManagement.Query.GetExitTicketForAdmin;

namespace NPark.Application.Feature.TicketsManagement.Command.ScanQrCode
{
    public sealed record ScanQrCodeCommand : ICommand<GetExitTicketForAdminQueryResponse>
    {
        public string QrCode { get; init; } = string.Empty;
    }
}