using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.TicketsManagement.Query.ValidateExitByVehicleNumber
{
    public sealed record ValidateExitByVehicleNumberQuery(string VehicleNumber)
      : IQuery<ValidateExitByVehicleNumberQueryResponse>;
}