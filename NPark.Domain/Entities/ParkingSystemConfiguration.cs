using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;

namespace NPark.Domain.Entities
{
    public sealed class ParkingSystemConfiguration : Entity<int>
    {
        private List<ParkingGate> _parkingGates = new List<ParkingGate>(); // بوابات الباكيات>

        // ===== Basic Parking Setup =====
        public int EntryGatesCount { get; private set; } = 1;   // عدد بوابات الدخول

        public int ExitGatesCount { get; private set; } = 1;    // عدد بوابات الخروج
        public int? AllowedParkingSlots { get; private set; } = 100; // عدد الباكيات المسموح
        public TimeSpan? GracePeriod { get; private set; } = null!;

        // ===== Behavior Enums =====
        public PriceType PriceType { get; private set; } = PriceType.Exit;

        public VehiclePassengerData VehiclePassengerData { get; private set; } = VehiclePassengerData.ScannerId;
        public PrintType PrintType { get; private set; } = PrintType.QrCode;

        public Guid? PricingSchemaId { get; private set; }
        public PricingScheme? PricingScheme { get; private set; }
        public bool DateTimeFlag { get; private set; } = true;
        public bool TicketIdFlag { get; private set; } = true;
        public bool FeesFlag { get; private set; } = true;

        public IReadOnlyCollection<ParkingGate> ParkingGates => _parkingGates;

        private ParkingSystemConfiguration()
        { }

        public static ParkingSystemConfiguration Create(int entryGatesCount, int exitGatesCount, int? allowedParkingSlots, PriceType priceType, VehiclePassengerData vehiclePassengerData, PrintType printType, bool dateTimeFlag,
            bool ticketIdFlag, bool feesFlag, Guid? pricingSchemaId, TimeSpan? gracePeriod) => new ParkingSystemConfiguration()
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

        public void Update(ParkingSystemConfiguration configuration, int entryGatesCount, int exitGatesCount,
            int? allowedParkingSlots, PriceType priceType, VehiclePassengerData vehiclePassengerData,
            PrintType printType, bool dateTimeFlag, bool ticketIdFlag, bool feesFlag, Guid? pricingSchemaId, TimeSpan? gracePeriod)
        {
            configuration.EntryGatesCount = entryGatesCount;
            configuration.ExitGatesCount = exitGatesCount;
            configuration.AllowedParkingSlots = allowedParkingSlots;
            configuration.PriceType = priceType;
            configuration.VehiclePassengerData = vehiclePassengerData;
            configuration.PrintType = printType;
            configuration.DateTimeFlag = dateTimeFlag;
            configuration.TicketIdFlag = ticketIdFlag;
            configuration.FeesFlag = feesFlag;
            configuration.GracePeriod = gracePeriod;
            configuration.PricingSchemaId = pricingSchemaId;
        }

        public void SetParkingGates(List<ParkingGate> parkingGates)
        {
            _parkingGates = parkingGates;
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