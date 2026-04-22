using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Specifications.ParkingGateSpec;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.FileNames;

namespace NPark.Application.Feature.UserManagement.Query.GetCashier
{
    internal sealed class GetCashierQueryHandler(IGenericRepository<User> userRepo)
        : IQueryHandler<GetCashierQuery, IReadOnlyCollection<GetCashierQueryResponse>>
    {
        public async Task<Result<IReadOnlyCollection<GetCashierQueryResponse>>> Handle(GetCashierQuery request, CancellationToken cancellationToken)
        {
            if (request.GateType == GateType.Entrance)
            {
                var entraneSpec = new GetCashierSpec(SystemRoles.EntranceCashierRoleId);
                var response = await userRepo.ListWithSpecAsync(entraneSpec, cancellationToken);
                return Result<IReadOnlyCollection<GetCashierQueryResponse>>.Ok(response);
            }

            if (request.GateType == GateType.Exit)
            {
                var exitSpec = new GetCashierSpec(SystemRoles.ExitCashierRoleId);
                var response = await userRepo.ListWithSpecAsync(exitSpec, cancellationToken);
                return Result<IReadOnlyCollection<GetCashierQueryResponse>>.Ok(response);
            }
            return Result<IReadOnlyCollection<GetCashierQueryResponse>>.Ok(new List<GetCashierQueryResponse>());
        }
    }
}