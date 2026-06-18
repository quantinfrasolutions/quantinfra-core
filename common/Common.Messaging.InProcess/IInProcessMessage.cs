namespace QuantInfra.Common.Messaging.InProcess;

public interface IInProcessMessage
{
    public string? Payload { get; set; }
    public object? Data { get; set; }
}