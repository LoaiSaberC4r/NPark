namespace NPark.Api.Contracts
{
    public sealed class OpenGateRequest
    {
        public string? OperationType { get; init; }
        public Guid? TicketId { get; init; }
        public string ControllerId { get; init; } = string.Empty;
    }
}