using Microsoft.Extensions.Options;
using NPark.Application.Options;

namespace NPark.Application.Services;

public sealed class ParkingBusinessClock(TimeProvider time, IOptions<ParkingBusinessOptions> options)
{
    private readonly TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);

    public DateTime UtcNow => time.GetUtcNow().UtcDateTime;
    public DateTime ToBusinessTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);
    public DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone);
    public DateTime LocalNow => ToBusinessTime(UtcNow);
    public DateOnly BusinessDate => DateOnly.FromDateTime(LocalNow);
}
