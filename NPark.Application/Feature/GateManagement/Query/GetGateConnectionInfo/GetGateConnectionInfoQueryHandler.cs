using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.GateManagement.Query.GetGateConnectionInfo
{
    internal sealed class GetGateConnectionInfoQueryHandler
        : IQueryHandler<GetGateConnectionInfoQuery, GetGateConnectionInfoQueryResponse>
    {
        private readonly IGenericRepository<ParkingGate> _reader;

        public GetGateConnectionInfoQueryHandler(IGenericRepository<ParkingGate> reader)
        {
            _reader = reader;
        }

        public async Task<Result<GetGateConnectionInfoQueryResponse>> Handle(
            GetGateConnectionInfoQuery request,
            CancellationToken cancellationToken)
        {
            var result = await _reader.FirstOrDefaultWithSpecAsync
                (new ParkingGateConnectionInfoReaderSpec(request.GateId), cancellationToken);

            if (result is null)
            {
                return Result<GetGateConnectionInfoQueryResponse>
                    .Fail(GetGateConnectionInfoErrors.GateNotFound);
            }

            return Result<GetGateConnectionInfoQueryResponse>.Ok(result);
        }
    }
}