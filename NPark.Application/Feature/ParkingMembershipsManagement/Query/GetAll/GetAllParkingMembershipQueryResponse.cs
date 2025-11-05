namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.GetAll
{
    public class GetAllParkingMembershipQueryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public List<GetAllParkingAttachment>? VehicleImage { get; set; } = new();
        public string VehicleNumber { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public Guid PricingSchemeId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime EndDate { get; set; }
    }

    public record GetAllParkingAttachment
    {
        public Guid Id { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }
}