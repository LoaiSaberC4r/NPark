using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Application.Abstraction.Security;
using CRM.Application.Specification.TicketSpecification;
using CRM.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace CRM.Application.Feature.TicketManagement.Query.GetAccountTicketCount
{
    internal sealed class GetAccountTicketCountQueryHandler : IQueryHandler<GetAccountTicketCountQuery, GetAccountTicketCountQueryResponse>
    {
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenReader _tokenReader;

        public GetAccountTicketCountQueryHandler(IGenericRepository<Account> accountRepository,
            ITokenReader tokenReader, IHttpContextAccessor httpContextAccessor)
        {
            _accountRepository = accountRepository;
            _tokenReader = tokenReader;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<GetAccountTicketCountQueryResponse>> Handle(GetAccountTicketCountQuery request, CancellationToken cancellationToken)
        {
            var tokenInfo = _httpContextAccessor.HttpContext?.ReadToken(_tokenReader);
            if (tokenInfo is null || !tokenInfo.UserId.HasValue)
            {
                return Result<GetAccountTicketCountQueryResponse>.Fail(
                    new Error(
                        Code: "Token.GateOrUser.NotFound",
                        Message: "Token Missing Or User Not Found",
                        Type: ErrorType.NotFound));
            }
            var spec = new GetAccountTicketCountSpec(tokenInfo.UserId.Value);
            var count = await _accountRepository.FirstOrDefaultWithSpecAsync(spec, cancellationToken);

            return Result<GetAccountTicketCountQueryResponse>.Ok(count ?? new GetAccountTicketCountQueryResponse { TicketCount = 0 });
        }
    }
}