using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Common.Backtesting.Abstractions;

public interface ITestMarketDataProvider : IMarketDataHistoryProvider, IClock
{
    ExchangeBar? GetNextBar();
    void Restart();
}