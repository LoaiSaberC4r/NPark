using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.GateManagement.Query.GetGateConnectionInfo
{
    public sealed record GetGateConnectionInfoQuery(Guid GateId)
        : IQuery<GetGateConnectionInfoQueryResponse>;
}