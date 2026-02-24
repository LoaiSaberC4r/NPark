using CRM.Application.Shared.Dto;
using CRM.Domain.Entities;

namespace CRM.Application.Abstraction.Security;

public interface IJwtProvider
{
    Task<UserTokenDto> Generate(SuperAdmin user, CancellationToken cancellationToken = default);

    Task<UserTokenDto> GenerateAccount(Account user, CancellationToken cancellationToken = default);

    Task<UserTokenDto> GenerateSupporter(Supporter user, CancellationToken cancellationToken = default);
}