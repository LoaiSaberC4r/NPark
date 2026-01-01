using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.AccountManagement.Query.GetSupporters
{
    public sealed record GetSupportersQuery : IQuery<IReadOnlyList<GetSupportersQueryResponse>>
    {
    }
}