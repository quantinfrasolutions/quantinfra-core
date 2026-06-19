using NodaTime;

namespace QuantInfra.Common.Interfaces.Api;

public static class InstantExtensions
{
    public static long ToApiFormat(this Instant instant) => instant.ToUnixTimeMilliseconds();
    public static long? ToApiFormat(this Instant? instant) => instant?.ToUnixTimeMilliseconds();
    public static Instant? FromApiFormat(this long? ts) => ts.HasValue ? Instant.FromUnixTimeMilliseconds(ts.Value) : null;
    
    public static long? ParseInput(this DateTime? dt)
    {
        if (!dt.HasValue) return null;
        return dt.Value.ParseInput();
    }
    
    public static long ParseInput(this DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc) dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return Instant.FromDateTimeUtc(dt).ToUnixTimeMilliseconds();
    }
    
    public static DateTime ToDateTimeUtc(this long ts) => Instant.FromUnixTimeMilliseconds(ts).ToDateTimeUtc();
}