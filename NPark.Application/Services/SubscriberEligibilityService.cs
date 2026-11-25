using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Application.Feature.ParkingMembershipsManagement.Query.GetSummaryById;
using NPark.Application.Feature.TicketsManagement.Command.Add;
using NPark.Application.Specifications.ParkingMembershipSpecification;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Services;

public sealed class SubscriberEligibilityService(IUnitOfWork db)
{
    public async Task<Result<GetCardSummaryByIdQueryResponse>> EvaluateAsync(
        string? cardNumber, string? vehicleNumber, DateTime businessTime, CancellationToken ct)
    {
        var spec = new GetCardSummaryByIdSpec(cardNumber ?? string.Empty, vehicleNumber ?? string.Empty);
        var subscriber = await db.FirstOrDefaultWithSpecAsync(spec, ct);
        if (subscriber is null)
            return Result<GetCardSummaryByIdQueryResponse>.Fail(
                new Error("Subscriber.Card.NotFound", "المشترك غير موجود ", ErrorType.NotFound));

        if (businessTime < subscriber.CreatedAt || businessTime >= subscriber.EndDate)
            return Result<GetCardSummaryByIdQueryResponse>.Fail(
                new Error("Subscriber.Card.InvalidDate", ErrorMessage.Subscriber_Card_InvalidDate, ErrorType.Validation));

        if (await db.AnyWithSpecAsync(new GetActiveSubscriberTicketSpec(
            subscriber.CardNumber, subscriber.NationalId, subscriber.VehicleNumber), ct))
            return Result<GetCardSummaryByIdQueryResponse>.Fail(
                new Error("Subscriber.AlreadyInsidePark", ErrorMessage.Subscriber_AlreadyInsidePark, ErrorType.Validation));

        return Result<GetCardSummaryByIdQueryResponse>.Ok(subscriber);
    }

    public async Task<bool> CanLockForVehicleAsync(string vehicleNumber, DateTime businessTime, CancellationToken ct)
    {
        // Use the existing membership plate comparison without allowing a blank card to match.
        var normalized = vehicleNumber.Trim().Replace(" ", "");
        if (normalized.Length == 0)
            return false;

        var memberships = await db.ListWithSpecAsync(new Matching<ParkingMemberships>(
            x => x.VehicleNumber.Trim().Replace(" ", "") == normalized, 2), ct);
        if (memberships.Count != 1)
            return false;

        var membership = memberships[0];
        return businessTime >= membership.CreatedAt && businessTime < membership.EndDate &&
            !await db.AnyWithSpecAsync(new GetActiveSubscriberTicketSpec(
                membership.CardNumber, membership.NationalId, membership.VehicleNumber), ct);
    }
}
