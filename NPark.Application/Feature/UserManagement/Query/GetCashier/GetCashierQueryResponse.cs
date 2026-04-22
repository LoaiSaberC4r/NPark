namespace NPark.Application.Feature.UserManagement.Query.GetCashier
{
    public sealed record GetCashierQueryResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}