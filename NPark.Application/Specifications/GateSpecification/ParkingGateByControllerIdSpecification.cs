using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Application.Specifications.GateSpecification
{
    internal sealed class ParkingGateByControllerIdSpecification : Specification<ParkingGate>
    {
        public ParkingGateByControllerIdSpecification(int controllerId)
        {
            // Assumption:
            // MQTT controllerId/deviceId maps to ParkingGate.GateNumber.
            // لو عندك Property اسمها ControllerId أو ControlId في ParkingGate
            AddCriteria(x => x.ControlIp == controllerId.ToString());

            // This flow is for exit only.
            AddCriteria(x => x.GateType == GateType.Exit);
        }
    }
}