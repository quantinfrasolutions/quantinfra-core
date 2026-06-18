namespace QuantInfra.Common.Interfaces.Api.Strategies;

public class StopStrategyRequest
{
    public string Reason { get; set; }
    public bool ClosePositions { get; set; }
}