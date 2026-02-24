namespace NPark.Application.Shared.Dto
{
    public sealed record SanctionDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal Amount { get; init; }
    }
}