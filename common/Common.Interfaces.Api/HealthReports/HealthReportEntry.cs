namespace QuantInfra.Common.Interfaces.Api.HealthReports
{
	public record HealthReportEntry
	{
        public Dictionary<string, object>? Data { get; set; }
        public string? Description { get; set; }
        public Exception? Exception { get; set; }
        public HealthStatus Status { get; set; }
        public IEnumerable<string>? Tags { get; set; }
    }
}

