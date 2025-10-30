using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.Media;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.FileNames;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Command.Add
{
    public sealed class AddParkingMembershipCommandHandler : ICommandHandler<AddParkingMembershipCommand>
    {
        private readonly IGenericRepository<ParkingMemberships> _parkingrepository;
        private readonly IGenericRepository<PricingScheme> _prinicngrepository;
        private readonly IMediaService _mediaService;

        public AddParkingMembershipCommandHandler(IGenericRepository<PricingScheme> prinicngrepository, IGenericRepository<ParkingMemberships> parkingrepository, IMediaService mediaService)
        {
            _prinicngrepository = prinicngrepository ?? throw new ArgumentNullException(nameof(prinicngrepository));
            _parkingrepository = parkingrepository ?? throw new ArgumentNullException(nameof(parkingrepository));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        }

        public async Task<Result> Handle(AddParkingMembershipCommand request, CancellationToken cancellationToken)
        {
            var pricingEntity = await _prinicngrepository.GetByIdAsync(request.PricingSchemeId, cancellationToken);
            DateTime endDate = DateTime.UtcNow;
            if (pricingEntity!.DurationType == DurationType.Days)
            {
                endDate = DateTime.UtcNow.AddDays(pricingEntity.TotalDays ?? 0);
            }
            else
            {
                endDate = DateTime.UtcNow.AddHours(pricingEntity.TotalHours ?? 0);
            }
            var filePath = string.Empty;
            if (request.VehicleImage is not null)
            {
                filePath = await _mediaService.SaveAsync(request.VehicleImage, FileNames.ParkingMemberships);
            }
            var parkingMemberships = ParkingMemberships.Create(
                request.Name,
                request.Phone,
                request.NationalId,
                filePath,
                request.VehicleNumber,
                request.CardNumber,
                request.PricingSchemeId,
                DateTime.UtcNow,
                endDate);
            await _parkingrepository.AddAsync(parkingMemberships, cancellationToken);
            await _parkingrepository.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
    }
}