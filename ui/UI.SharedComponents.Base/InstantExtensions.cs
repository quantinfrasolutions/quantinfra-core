using NodaTime;

namespace UI.SharedComponents.Base;

public static class InstantExtensions
{
    public static Instant? ParseInput(this DateTime? dt)
    {
        if (!dt.HasValue) return null;
        return dt.Value.ParseInput();
    }
    
    public static Instant ParseInput(this DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc) dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return Instant.FromDateTimeUtc(dt);
    }
}