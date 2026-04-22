using BuildingBlock.Application.Abstraction;
using NPark.Domain.Enums;

namespace NPark.Application.Feature.UserManagement.Query.GetCashier
{
    public sealed record GetCashierQuery : IQuery<IReadOnlyCollection<GetCashierQueryResponse>>
    {
        public GateType GateType { get; init; }
    }
}