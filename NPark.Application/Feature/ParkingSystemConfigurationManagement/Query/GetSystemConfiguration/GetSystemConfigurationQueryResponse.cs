using NPark.Application.Feature.ParkingSystemConfigurationManagement.Command.Update;
using NPark.Domain.Enums;

namespace NPark.Application.Feature.ParkingSystemConfigurationManagement.Query.GetSystemConfiguration
{
    public sealed record GetSystemConfigurationQueryResponse
    {
        public int EntryGatesCount { get; set; }
        public int ExitGatesCount { get; set; }
        public int? AllowedParkingSlots { get; set; }

        public PriceType PriceType { get; set; }
        public int? gracePeriodMinutes { get; set; }
        public VehiclePassengerData VehiclePassengerData { get; set; }
        public PrintType PrintType { get; set; }

        public bool DateTimeFlag { get; set; }
        public bool TicketIdFlag { get; set; }
        public bool FeesFlag { get; set; }
        public Guid? PricingSchemaId { get; set; }
        public string PricingSchemaName { get; set; } = string.Empty;
        public List<GateInfo> EntryGates { get; set; } = new();
        public List<GateInfo> ExitGates { get; set; } = new();
    }
}