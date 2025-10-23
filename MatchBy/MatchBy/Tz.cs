using TimeZoneConverter;

namespace MatchBy;

internal static class Tz
{
    public static TimeZoneInfo FromId(string tzId) =>
        TZConvert.GetTimeZoneInfo(tzId); // accepts "Europe/Lisbon" or "GMT Standard Time"

    // UTC -> Local (in that timezone)
    public static DateTime ToLocal(DateTime utc, TimeZoneInfo tz) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);

    // Local -> UTC (value from DatePicker is "Unspecified")
    public static DateTime ToUtc(DateTime local, TimeZoneInfo tz)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
    }
}
