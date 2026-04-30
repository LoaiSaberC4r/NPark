using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.MemberShipExist
{
    internal sealed class MemberShipExistQueryHandler
        : IQueryHandler<MemberShipExistQuery, MemberShipExistQueryResponse>
    {
        private readonly IGenericRepository<ParkingMemberships> _repository;

        public MemberShipExistQueryHandler(IGenericRepository<ParkingMemberships> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<MemberShipExistQueryResponse>> Handle(
            MemberShipExistQuery request,
            CancellationToken cancellationToken)
        {
            var searchValue = request.SearchValue.Trim();

            var exists = await _repository.IsExistAsync(
    x => x.VehicleNumber.Trim().Replace(" ", "") == searchValue.Trim().Replace(" ", "") ||
         x.CardNumber.Trim() == searchValue,
    cancellationToken);

            return Result<MemberShipExistQueryResponse>.Ok(new MemberShipExistQueryResponse
            {
                IsExist = exists
            });
        }
    }
}