namespace NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText
{
    public sealed record ValidateExitByTextCommandResponse
    {
        public Guid? TicketId { get; init; }
        public string InputText { get; init; } = string.Empty;
        public string? MatchType { get; init; } // QrCode | CardNumber | VehicleNumber
        public string? TicketNumber { get; init; }
        public string? CardNumber { get; init; }
        public string? VehicleNumber { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
        public bool IsValidForExit { get; init; }
    }
}