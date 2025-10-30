using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.SharedDto;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.GetAll
{
    public sealed class GetAllParkingMembershipQuery : SearchParameters, IQuery<Pagination<GetAllParkingMembershipQueryResponse>>
    {
    }
}