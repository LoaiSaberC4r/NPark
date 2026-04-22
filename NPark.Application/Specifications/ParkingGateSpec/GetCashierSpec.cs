using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.UserManagement.Query.GetCashier;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.ParkingGateSpec
{
    internal sealed class GetCashierSpec : Specification<User, GetCashierQueryResponse>
    {
        public GetCashierSpec(Guid role)
        {
            AddCriteria(x => x.RoleId == role);
            Select(x => new GetCashierQueryResponse
            {
                Id = x.Id,
                Name = x.Name
            });
        }
    }
}