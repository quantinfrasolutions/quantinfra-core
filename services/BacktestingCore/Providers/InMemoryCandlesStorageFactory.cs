// using BacktestingCore.Providers;
// using Microsoft.Extensions.Logging;
// using NodaTime;
// using QuantInfra.Common.Backtesting.Abstractions;
// using QuantInfra.Common.MarketData.Abstractions;
//
// namespace QuantInfra.Services.BacktestingCore.Providers;
//
// public class InMemoryCandlesStorageFactory : ITestMarketDataProviderFactory
// {
//     private readonly bool _useCache;
//     private IMarketDataHistoryProvider _historyProvider;
//     private ILogger<InMemoryCandlesStorage> _logger;
//
//     public InMemoryCandlesStorageFactory(bool useCache, IMarketDataHistoryProvider historyProvider, ILoggerFactory loggerFactory)
//     {
//         _useCache = useCache;
//         _historyProvider = historyProvider;
//         _logger = loggerFactory.CreateLogger<InMemoryCandlesStorage>();
//     }
//     
//     public ITestMarketDataProvider GetInstance(Instant fromDt, Instant toDt, Duration timeframe) =>
//         new InMemoryCandlesStorage(new InMemoryCandlesStorageOptions(fromDt, toDt, timeframe, _useCache), _historyProvider, _logger);
// }