using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.MemberShipExistByVehicleNumber
{
    internal sealed class MemberShipExistByVehicleNumberQueryHandler
        : IQueryHandler<MemberShipExistByVehicleNumberQuery, MemberShipExistByVehicleNumberQueryResponse>
    {
        private readonly IGenericRepository<ParkingMemberships> _repository;

        public MemberShipExistByVehicleNumberQueryHandler(IGenericRepository<ParkingMemberships> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<MemberShipExistByVehicleNumberQueryResponse>> Handle(MemberShipExistByVehicleNumberQuery request, CancellationToken cancellationToken)
        {
            var spec = new MemberShipExistByVehicleNumberSpec(request.VehicleNumber);

            var exists = await _repository.IsExistAsync(x => x.VehicleNumber == request.VehicleNumber, cancellationToken);
            var response = new MemberShipExistByVehicleNumberQueryResponse
            {
                IsExist = exists
            };

            return Result<MemberShipExistByVehicleNumberQueryResponse>.Ok(response);
        }
    }
}