using QuantInfra.Common.Interfaces.Api.HealthReports;

namespace QuantInfra.Common.Utils.ExecutableAppBase
{
	public class HealthCheckResponseWriter
	{
        public static Task WriteResponse(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport healthReport)
        {
            context.Response.StatusCode = 200;
            return context.Response.WriteAsJsonAsync(
                new HealthReport
                {
                    Status = ConvertStatus(healthReport.Status),
                    Entries = healthReport.Entries.ToDictionary(
                        e => e.Key,
                        e => new HealthReportEntry
                        {
                            Data = e.Value.Data?.ToDictionary(d => d.Key, d => d.Value),
                            Description = e.Value.Description,
                            Status = ConvertStatus(e.Value.Status),
                            Tags = e.Value.Tags.ToList()
                        }
                    )
                }
            );
        }

        public static Task WriteResponseForKubernetes(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport healthReport)
        {
            if (healthReport.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy)
            {
                context.Response.StatusCode = 503;
            }
            else
            {
                context.Response.StatusCode = 200;
            }

            return Task.CompletedTask;
        }

        static HealthStatus ConvertStatus(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus status) => status switch
        {
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => HealthStatus.Degraded,
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy => HealthStatus.Healthy,
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy => HealthStatus.Unhealthy
        };
    }
}

