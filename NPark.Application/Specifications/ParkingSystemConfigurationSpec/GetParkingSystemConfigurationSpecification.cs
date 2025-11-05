using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.ParkingSystemConfigurationManagement.Query.GetSystemConfiguration;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.ParkingSystemConfigurationSpec
{
    public sealed class GetParkingSystemConfigurationSpecification : Specification<ParkingSystemConfiguration, GetSystemConfigurationQueryResponse>
    {
        public GetParkingSystemConfigurationSpecification()
        {
            AddCriteria(x => x.Id == 1);
            Include(x => x.PricingScheme);
            Select(x => new GetSystemConfigurationQueryResponse
            {
                AllowedParkingSlots = x.AllowedParkingSlots,
                EntryGatesCount = x.EntryGatesCount,
                ExitGatesCount = x.ExitGatesCount,
                PriceType = x.PriceType,
                VehiclePassengerData = x.VehiclePassengerData,
                PrintType = x.PrintType,
                DateTimeFlag = x.DateTimeFlag,
                TicketIdFlag = x.TicketIdFlag,
                FeesFlag = x.FeesFlag,
                GracePeriod = x.GracePeriod,
                PricingSchemaId = x.PricingSchemaId,
                PricingSchemaName = (x.PricingScheme == null) ? string.Empty : x.PricingScheme.Name
            });
        }
    }
}