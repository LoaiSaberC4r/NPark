using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public class ParkingMemberships : Entity<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        public string Phone { get; private set; } = string.Empty;
        public string NationalId { get; private set; } = string.Empty;
        public string? VehicleImage { get; private set; } = string.Empty;
        public string VehicleNumber { get; private set; } = string.Empty;
        public int CardNumber { get; private set; }
        public Guid PricingSchemeId { get; private set; }
        public PricingScheme PricingScheme { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }
        public DateTime EndDate { get; private set; }

        private ParkingMemberships()
        { }

        public static ParkingMemberships Create(string name, string phone, string nationalId, string? vehicleImage, string vehicleNumber, int cardNumber, Guid pricingSchemeId, DateTime createdAt, DateTime endDate) => new ParkingMemberships()
        {
            Name = name,
            Phone = phone,
            NationalId = nationalId,
            VehicleImage = vehicleImage,
            VehicleNumber = vehicleNumber,
            CardNumber = cardNumber,
            PricingSchemeId = pricingSchemeId,
            CreatedAt = createdAt,
            EndDate = endDate
        };

        public void UpdateEndDate(DateTime endDate) => EndDate = endDate;

        public void UpdateVehicleImage(string vehicleImage) => VehicleImage = vehicleImage;

        public void UpdateVehicleNumber(string vehicleNumber) => VehicleNumber = vehicleNumber;

        public void UpdateCardNumber(int cardNumber) => CardNumber = cardNumber;

        public void UpdatePricingSchemeId(Guid pricingSchemeId) => PricingSchemeId = pricingSchemeId;

        public void UpdateName(string name) => Name = name;

        public void UpdatePhone(string phone) => Phone = phone;

        public void UpdateNationalId(string nationalId) => NationalId = nationalId;

        public void UpdateCreatedAt(DateTime createdAt) => CreatedAt = createdAt;
    }
}