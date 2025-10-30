namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.GetAll
{
    public class GetAllParkingMembershipQueryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string? VehicleImage { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public int CardNumber { get; set; }
        public Guid PricingSchemeId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime EndDate { get; set; }
    }
}