using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText;
using NPark.Application.Services;

namespace NPark.Application.Feature.TicketsManagement.Query.ValidateExitByVehicleNumber;

internal sealed class ValidateExitByVehicleNumberQueryHandler(IUnitOfWork db,
    ITicketExitEligibilityService eligibility)
    : IQueryHandler<ValidateExitByVehicleNumberQuery, ValidateExitByVehicleNumberQueryResponse>
{
    public async Task<Result<ValidateExitByVehicleNumberQueryResponse>> Handle(ValidateExitByVehicleNumberQuery request, CancellationToken ct)
    {
        var tickets = await db.ListWithSpecAsync(new TicketByVehicleNumberSpecification(request.VehicleNumber), ct);
        if (tickets.Count > 1)
            return Result<ValidateExitByVehicleNumberQueryResponse>.Ok(new() { ReasonCode = "AmbiguousTicket" });
        var ticket = tickets.SingleOrDefault();
        var assessment = await eligibility.EvaluateAsync(ticket, null, ct);
        return Result<ValidateExitByVehicleNumberQueryResponse>.Ok(new()
        {
            TicketId = ticket?.Id, VehicleNumber = ticket?.VehicleNumber, StartDate = ticket?.StartDate,
            IsValidForExit = assessment.IsValidForExit,
            ReasonCode = assessment.ReasonCode, Eligibility = assessment
        });
    }
}
