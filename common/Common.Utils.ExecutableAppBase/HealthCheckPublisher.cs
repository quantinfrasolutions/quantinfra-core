using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace ExecutableAppBase;

public class HealthCheckPublisher : IHealthCheckPublisher
{
    private static readonly Dictionary<string, Gauge> Gauges = new();

    public static Gauge GetGauge(string name)
    {
        name = name.ToLowerInvariant().Replace(' ', '_');
        if (!Gauges.ContainsKey(name))
        {
            Gauges.Add(name, Metrics
                .CreateGauge(
                    name,
                    "Application health status (1=healthy, 0=unhealthy, 0.5=degraded). Message included as a label.",
                    new GaugeConfiguration
                    {
                        LabelNames = new[] { "message" }
                    })
            );
        }

        return Gauges[name];
    }
    
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var entry in report.Entries)
        {
            var gauge = GetGauge(entry.Key);
            switch (entry.Value.Status)
            {
                case HealthStatus.Degraded:
                    gauge.WithLabels(entry.Value.Description!).Set(0.5);
                    break;
                case HealthStatus.Unhealthy:
                    gauge.WithLabels(entry.Value.Description!).Set(0);
                    break;
                default:
                    gauge.WithLabels("OK").Set(1);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}