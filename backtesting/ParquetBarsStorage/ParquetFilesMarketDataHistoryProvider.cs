using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;

// ReSharper disable InvalidXmlDocComment

namespace QuantInfra.Backtesting.ParquetBarsStorage;

/// <summary>
/// Allows reading historical bars from multiple Parquet files
/// </summary>
public class ParquetFilesMarketDataHistoryProvider : IMarketDataHistoryProvider
{
    private IReadOnlyDictionary<int, ParquetFileMarketDataHistoryProvider> _providers;

    /// <param name="directoryPath"></param>
    /// <param name="contractsToStreams"></param>
    /// <param name="cTicker"></param>
    /// <param name="tf"></param>
    /// <param name="tradingSessions">ContractId => Trading sessions</param>
    /// <param name="useContractIds"></param>
    public ParquetFilesMarketDataHistoryProvider(
        string directoryPath,
        IReadOnlyDictionary<int, int> contractsToStreams,
        Duration? tf = null,
        IReadOnlyDictionary<int, IReadOnlyCollection<TradingSession>>? tradingSessions = null
    )
    {
        tf ??= Duration.FromMinutes(1);

        _providers = contractsToStreams.ToDictionary(
            kv => kv.Key,
            kv => new ParquetFileMarketDataHistoryProvider(
                Path.Join(directoryPath, $"{kv.Key}.parquet"),
                kv.Value,
                kv.Key,
                tf,
                tradingSessions
            )
        );
    }
    

    public IReadOnlyDictionary<int, double> GetLastKnownPrices(IEnumerable<int> contractIds, Instant dt)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ExchangeBar> GetBAUsByStream(int streamId, Instant from, Instant to)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to) =>
        _providers[contractId].GetBAUsByContract(contractId, from, to);

    public IEnumerable<ExchangeBar> GetAggregatedCandlesByContract(int contractId, Instant from, Instant to,
        Period timeframe, string timezone)
    {
        return _providers[contractId].GetBAUsByContract(contractId, from, to);
    }

    public IEnumerable<ExchangeBar> GetAggregatedBausByStream(int streamId, Instant from, Instant to, Period timeframe,
        string timezone)
    {
        throw new NotImplementedException();
    }
}