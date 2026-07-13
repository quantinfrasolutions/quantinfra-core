namespace QuantInfra.Common.Infrastructure.Abstractions;

public class HostedComponentStatus
{
    public HostedComponentStatus(string componentName, ComponentStatus status, string? exception)
    {
        ComponentName = componentName;
        Status = status;
        Exception = exception;
    }

    public string ComponentName { get; init; }
    public ComponentStatus Status { get; init; }
    public string? Exception { get; init; }
}