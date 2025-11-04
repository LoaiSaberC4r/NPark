using BuildingBlock.Application.Abstraction;

namespace NPark.Application.Feature.ParkingSystemConfigurationManagement.Query.GetSystemConfiguration
{
    public sealed record GetSystemConfigurationQuery : IQuery<GetSystemConfigurationQueryResponse>
    {
    }
}