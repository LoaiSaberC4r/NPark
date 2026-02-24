using BuildingBlock.Application.Abstraction;
using Microsoft.AspNetCore.Http;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Command.Update
{
    public sealed record UpdateParkingMembershipCommand : ICommand
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string NationalId { get; init; } = string.Empty;

        public string VehicleNumber { get; init; } = string.Empty;
        public string CardNumber { get; init; } = string.Empty;

        public Guid PricingSchemeId { get; init; }

        public List<IFormFile>? NewVehicleImages { get; init; }

        // AttachmentIds تتشال
        public List<Guid>? RemoveAttachmentIds { get; init; }
    }
}