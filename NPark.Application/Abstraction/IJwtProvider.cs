using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;

namespace NPark.Application.Abstraction
{
    public interface IJwtProvider
    {
        Task<UserTokenDto> Generate(User user, ParkingGate? gate = null, CancellationToken cancellationToken = default);
    }
}