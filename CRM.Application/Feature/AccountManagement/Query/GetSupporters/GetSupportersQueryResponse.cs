namespace CRM.Application.Feature.AccountManagement.Query.GetSupporters
{
    public sealed record GetSupportersQueryResponse
    {
        public string Email { get; init; } = string.Empty;
        public Guid Id { get; init; }
    }
}