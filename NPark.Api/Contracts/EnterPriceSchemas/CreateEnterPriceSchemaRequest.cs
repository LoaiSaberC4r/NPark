namespace NPark.Api.Contracts.EnterPriceSchemas
{
    public sealed record CreateEnterPriceSchemaRequest
    {
        public string Name { get; init; } = string.Empty;
        public decimal Salary { get; init; }
    }
}