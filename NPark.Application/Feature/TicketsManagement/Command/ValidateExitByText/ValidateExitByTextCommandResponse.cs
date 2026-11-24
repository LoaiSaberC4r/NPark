using NPark.Application.Services;
namespace NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText
{
    public sealed record ValidateExitByTextCommandResponse : ExitValidationResponse
    {
        public string InputText { get; init; } = string.Empty;
        public string? MatchType { get; init; } // QrCode | CardNumber | VehicleNumber
        public string? TicketNumber { get; init; }
        public string? CardNumber { get; init; }
        public DateTime? EndDate { get; init; }
    }
}