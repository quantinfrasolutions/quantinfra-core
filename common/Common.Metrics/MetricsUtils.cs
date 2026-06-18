using System.Diagnostics;

namespace Common.Metrics;

public static class MetricsUtils
{
    public static long GetUnixMicro() => Stopwatch.GetTimestamp() * 1_000_000 / Stopwatch.Frequency;
}