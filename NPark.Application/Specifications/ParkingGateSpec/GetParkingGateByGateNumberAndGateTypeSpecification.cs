using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Application.Specifications.ParkingGateSpec
{
    public class GetParkingGateByGateNumberAndGateTypeSpecification : Specification<ParkingGate>
    {
        public GetParkingGateByGateNumberAndGateTypeSpecification(int number, GateType gateType)
        {
            AddCriteria(x => x.GateNumber == number);
            AddCriteria(x => x.GateType == gateType);
        }
    }
}