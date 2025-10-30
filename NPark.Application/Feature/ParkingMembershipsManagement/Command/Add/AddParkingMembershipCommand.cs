using BuildingBlock.Application.Abstraction;
using Microsoft.AspNetCore.Http;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Command.Add
{
    public sealed record AddParkingMembershipCommand : ICommand
    {
        public string Name { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public IFormFile? VehicleImage { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public int CardNumber { get; set; }
        public Guid PricingSchemeId { get; set; }
    }
}