using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Backtesting.Abstractions;

public interface IMarketDataStorage
{
    Task<IReadOnlyCollection<RequiredMarketDataUnit>> ValidateRequiredMarketData(
        IReadOnlyCollection<RequiredMarketDataUnit> requiredMarketData, Period? tf = null);

    IMarketDataHistoryProvider CreateMarketDataHistoryProvider(IReadOnlyCollection<RequiredMarketDataUnit> reqs,
        IReadOnlyDictionary<int,IReadOnlyCollection<TradingSession>>? tradingSessions, Period? tf = null);
    
    bool AllowsRawDataInDifferentTimeframes { get; }
}