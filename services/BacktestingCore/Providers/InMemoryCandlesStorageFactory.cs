using Common.Backtesting;
using Common.MarketData;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;

namespace BacktestingCore.Providers;

public class InMemoryCandlesStorageFactory : ITestMarketDataProviderFactory
{
    private readonly bool _useCache;
    private IMarketDataHistoryProvider _historyProvider;
    private ILogger<InMemoryCandlesStorage> _logger;

    public InMemoryCandlesStorageFactory(bool useCache, IMarketDataHistoryProvider historyProvider, ILoggerFactory loggerFactory)
    {
        _useCache = useCache;
        _historyProvider = historyProvider;
        _logger = loggerFactory.CreateLogger<InMemoryCandlesStorage>();
    }
    
    public ITestMarketDataProvider GetInstance(Instant fromDt, Instant toDt, Duration timeframe) =>
        new InMemoryCandlesStorage(new InMemoryCandlesStorageOptions(fromDt, toDt, timeframe, _useCache), _historyProvider, _logger);
}