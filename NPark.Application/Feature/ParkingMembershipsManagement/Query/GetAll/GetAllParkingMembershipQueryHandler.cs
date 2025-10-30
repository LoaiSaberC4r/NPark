using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.SharedDto;
using NPark.Application.Specifications.ParkingMembershipSpecification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.GetAll
{
    public sealed class GetAllParkingMembershipQueryHandler : IQueryHandler<GetAllParkingMembershipQuery, Pagination<GetAllParkingMembershipQueryResponse>>
    {
        private readonly IGenericRepository<ParkingMemberships> _parkingRepo;

        public GetAllParkingMembershipQueryHandler(IGenericRepository<ParkingMemberships> parkingRepo)
        {
            _parkingRepo = parkingRepo ?? throw new ArgumentNullException(nameof(parkingRepo));
        }

        public async Task<Result<Pagination<GetAllParkingMembershipQueryResponse>>> Handle(GetAllParkingMembershipQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetParkingMembershipWithPriceSchemaSpec(request);
            var result = _parkingRepo.GetWithSpec(spec);
            var response = new Pagination<GetAllParkingMembershipQueryResponse>(
                request.PageNumber,
                request.PageSize,
                result.count,
                result.data.ToList()
            );
            return Result<Pagination<GetAllParkingMembershipQueryResponse>>.Ok(response);
        }
    }
}