using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public class ParkingMembershipsAttachment : Entity<Guid>
    {
        public Guid ParkingMembershipsId { get; set; }
        public ParkingMemberships ParkingMemberships { get; set; } = null!;
        public string FilePath { get; set; } = string.Empty;

        private ParkingMembershipsAttachment()
        { }

        public static ParkingMembershipsAttachment Create(Guid parkingMembershipsId, string filePath) => new ParkingMembershipsAttachment()
        {
            ParkingMembershipsId = parkingMembershipsId,
            FilePath = filePath
        };
    }
}