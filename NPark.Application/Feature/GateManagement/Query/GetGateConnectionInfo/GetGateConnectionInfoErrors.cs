using BuildingBlock.Domain.Results;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.GateManagement.Query.GetGateConnectionInfo
{
    public static class GetGateConnectionInfoErrors
    {
        public static readonly Error GateNotFound = new(
            "ParkingGate.ConnectionInfo.NotFound",
            ErrorMessage.ParkingGateConnectionInfoNotFound,
            ErrorType.NotFound);
    }
}