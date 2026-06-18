using System.Diagnostics;

namespace QuantInfra.Connectors.Common.Metrics;

public static class MetricsUtils
{
    public static long GetUnixMicro() => Stopwatch.GetTimestamp() * 1_000_000 / Stopwatch.Frequency;
}