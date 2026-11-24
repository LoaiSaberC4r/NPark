using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Options;
using NPark.Application.Options;
using NPark.Domain.Entities;

namespace NPark.Application.Services;

public sealed record OvernightRequirement(DateOnly? From, DateOnly? Through, Sanction? Sanction, Error? Error = null)
{
    public int Count => From.HasValue && Through.HasValue ? Through.Value.DayNumber - From.Value.DayNumber + 1 : 0;
}

public sealed class OvernightPenaltyPolicy(IOptions<OvernightParkingOptions> options, IUnitOfWork db)
{
    public Guid? SanctionId => options.Value.OvernightSanctionId;

    public async Task<OvernightRequirement> GetRequirementAsync(Ticket ticket, DateOnly date, CancellationToken ct)
    {
        if (ticket.ValidExitDate >= date)
            return new(null, null, null);
        var from = options.Value.PenaltyMode == OvernightPenaltyMode.PerOverdueBusinessDay
            ? ticket.ValidExitDate.AddDays(1) : date;
        var sanction = options.Value.Enabled && SanctionId is Guid id
            ? await db.Repository<Sanction>().GetByIdAsync(id, ct) : null;
        return sanction is { Amount: > 0 }
            ? new(from, date, sanction)
            : new(from, date, null, new("OvernightPenaltyConfigurationMissing",
                WorkflowMessages.Get("OvernightPenaltyConfigurationMissing"), ErrorType.Conflict));
    }
}
