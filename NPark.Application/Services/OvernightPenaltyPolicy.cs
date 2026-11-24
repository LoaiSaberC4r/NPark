using NPark.Domain.Entities;

namespace NPark.Application.Services;

public sealed class OvernightPenaltyPolicy
{
    public bool RequiresPenalty(Ticket ticket, DateOnly businessDate) => ticket.ValidExitDate < businessDate;
}
