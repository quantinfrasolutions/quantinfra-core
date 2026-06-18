namespace QuantInfra.Common.Interfaces.Api.HealthReports
{
    public record HealthReport
	{
        public Dictionary<string, HealthReportEntry>? Entries { get; set; }
        public HealthStatus Status { get; set; }        
    }
}

