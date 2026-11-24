namespace NPark.Application.Feature.TicketsManagement.Command.Add
{
    public sealed record AddTicketCommandResponse
    {
        public DateOnly ValidExitDate { get; init; }
        public string EntryConfirmationStatus { get; init; } = string.Empty;
        public bool IsCashierCollected { get; init; }
        public byte[] QrCode { get; init; } = Array.Empty<byte>();
        public Guid TicketId { get; init; }
        public DateTime CreatedAt { get; init; }
        public decimal Price { get; init; }
        public string? TicketInfo { get; init; } = string.Empty;
    }
}