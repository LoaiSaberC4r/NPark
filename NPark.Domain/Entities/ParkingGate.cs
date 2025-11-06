using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;

namespace NPark.Domain.Entities
{
    public class ParkingGate : Entity<Guid>
    {
        public int GateNumber { get; private set; }
        public GateType GateType { get; private set; }
        public bool? IsOccupied { get; private set; }
        public Guid? OccupiedBy { get; private set; }
        public DateTime? OccupiedTime { get; private set; }
        public string? LprIp { get; private set; } = string.Empty;

        public int? ParkingSystemConfigurationId { get; private set; }
        public ParkingSystemConfiguration? ParkingSystemConfiguration { get; private set; }

        private ParkingGate()
        { }

        public static ParkingGate Create(int gateNumber, GateType gateType, string? lprIp = null, int? id = null) => new ParkingGate()
        {
            GateNumber = gateNumber,
            GateType = gateType,
            LprIp = lprIp,
            ParkingSystemConfigurationId = id
        };

        public void SetIsOccupied(bool isOccupied, Guid? occupiedBy = null, DateTime? occupiedTime = null)
        {
            IsOccupied = isOccupied;
            OccupiedBy = occupiedBy;
            OccupiedTime = occupiedTime;
        }
    }
}