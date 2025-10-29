using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public class ParkingMemberships : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string? VehicleImage { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public Guid PricingSchemeId { get; set; }
        public PricingScheme PricingScheme { get; set; } = null!;
        public int CardNumber { get; private set; }
    }
}