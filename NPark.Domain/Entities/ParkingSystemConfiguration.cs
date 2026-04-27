using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;

namespace NPark.Domain.Entities
{
    public sealed class ParkingSystemConfiguration : Entity<int>
    {
        private readonly List<ParkingGate> _parkingGates = new();

        public int EntryGatesCount { get; private set; } = 1;
        public int ExitGatesCount { get; private set; } = 1;
        public int? AllowedParkingSlots { get; private set; } = 100;
        public TimeSpan? GracePeriod { get; private set; }

        public PriceType PriceType { get; private set; } = PriceType.Exit;
        public VehiclePassengerData VehiclePassengerData { get; private set; } = VehiclePassengerData.ScannerId;
        public PrintType PrintType { get; private set; } = PrintType.QrCode;

        public Guid? PricingSchemaId { get; private set; }
        public PricingScheme? PricingScheme { get; private set; }

        public bool DateTimeFlag { get; private set; } = true;
        public bool TicketIdFlag { get; private set; } = true;
        public bool FeesFlag { get; private set; } = true;

        public IReadOnlyCollection<ParkingGate> ParkingGates => _parkingGates.AsReadOnly();

        private ParkingSystemConfiguration()
        {
        }

        public static ParkingSystemConfiguration Create(
            int entryGatesCount,
            int exitGatesCount,
            int? allowedParkingSlots,
            PriceType priceType,
            VehiclePassengerData vehiclePassengerData,
            PrintType printType,
            bool dateTimeFlag,
            bool ticketIdFlag,
            bool feesFlag,
            Guid? pricingSchemaId,
            TimeSpan? gracePeriod)
        {
            return new ParkingSystemConfiguration
            {
                EntryGatesCount = entryGatesCount,
                ExitGatesCount = exitGatesCount,
                AllowedParkingSlots = allowedParkingSlots,
                PriceType = priceType,
                VehiclePassengerData = vehiclePassengerData,
                PrintType = printType,
                DateTimeFlag = dateTimeFlag,
                TicketIdFlag = ticketIdFlag,
                FeesFlag = feesFlag,
                GracePeriod = gracePeriod,
                PricingSchemaId = pricingSchemaId
            };
        }

        public void Update(
            int entryGatesCount,
            int exitGatesCount,
            int? allowedParkingSlots,
            PriceType priceType,
            VehiclePassengerData vehiclePassengerData,
            PrintType printType,
            bool dateTimeFlag,
            bool ticketIdFlag,
            bool feesFlag,
            Guid? pricingSchemaId,
            TimeSpan? gracePeriod)
        {
            EntryGatesCount = entryGatesCount;
            ExitGatesCount = exitGatesCount;
            AllowedParkingSlots = allowedParkingSlots;
            PriceType = priceType;
            VehiclePassengerData = vehiclePassengerData;
            PrintType = printType;
            DateTimeFlag = dateTimeFlag;
            TicketIdFlag = ticketIdFlag;
            FeesFlag = feesFlag;
            GracePeriod = gracePeriod;
            PricingSchemaId = pricingSchemaId;
        }

        public void AddParkingGate(ParkingGate parkingGate)
        {
            _parkingGates.Add(parkingGate);
        }

        public void ClearParkingGates()
        {
            _parkingGates.Clear();
        }
    }
}