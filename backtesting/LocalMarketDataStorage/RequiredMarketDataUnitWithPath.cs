using QuantInfra.Common.Backtesting.Abstractions;

namespace QuantInfra.Backtesting.LocalMarketDataStorage;

public class RequiredMarketDataUnitWithPath : RequiredMarketDataUnit
{
    public RequiredMarketDataUnitWithPath(RequiredMarketDataUnit unit, string? path)
    {
        StreamId = unit.StreamId;
        StreamTicker = unit.StreamTicker;
        ContractId = unit.ContractId;
        Ticker = unit.Ticker;
        IsOk = unit.IsOk;
        Message = unit.Message;
        Path = path;
    }
    
    public string? Path { get; init; }
}