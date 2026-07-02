namespace QuantInfra.Common.Backtesting.Abstractions;

public class RequiredMarketDataUnit
{
    public int? StreamId { get; init; }
    public string? StreamTicker { get; init; }
    public int? ContractId { get; init; }
    public string? Ticker { get; init; }
    public bool IsOk { get; init; }
    public string? Message { get; init; }
    public bool DataRequired { get; init; } = true;
}