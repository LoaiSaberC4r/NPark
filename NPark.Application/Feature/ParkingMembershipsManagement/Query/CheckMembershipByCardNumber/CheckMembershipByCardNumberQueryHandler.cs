using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.ParkingMembershipsManagement.Query.CheckMembershipByCardNumber
{
    internal sealed class CheckMembershipByCardNumberQueryHandler
         : IQueryHandler<CheckMembershipByCardNumberQuery, CheckMembershipByCardNumberQueryResponse>
    {
        private readonly IGenericRepository<ParkingMemberships> _reader;

        public CheckMembershipByCardNumberQueryHandler(
            IGenericRepository<ParkingMemberships> reader)
        {
            _reader = reader;
        }

        public async Task<Result<CheckMembershipByCardNumberQueryResponse>> Handle(
            CheckMembershipByCardNumberQuery request,
            CancellationToken cancellationToken)
        {
            var cardNumber = request.CardNumber.Trim();

            var result = await _reader.FirstOrDefaultWithSpecAsync(
                new CheckMembershipByCardNumberSpec(cardNumber),
                cancellationToken);

            if (result is null)
            {
                return Result<CheckMembershipByCardNumberQueryResponse>.Ok(
                    new CheckMembershipByCardNumberQueryResponse
                    {
                        IsMember = false
                    });
            }

            return Result<CheckMembershipByCardNumberQueryResponse>.Ok(result);
        }
    }
}