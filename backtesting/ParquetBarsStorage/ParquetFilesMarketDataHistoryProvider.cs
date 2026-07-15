using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Common.Utils.Collections;
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
    private readonly IReadOnlyDictionary<int, int> _contractsToStreams;

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

        _contractsToStreams = contractsToStreams.Copy();

        _providers = contractsToStreams.ToDictionary(
            kv => kv.Value,
            kv => new ParquetFileMarketDataHistoryProvider(
                Path.Join(directoryPath, $"{kv.Value}.parquet"),
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

    public IEnumerable<ExchangeBar> GetBAUsByStream(int streamId, Instant from, Instant to) =>
        _providers[streamId].GetBAUsByStream(streamId, from, to);

    public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to) =>
        _providers[_contractsToStreams[contractId]].GetBAUsByContract(contractId, from, to);

    public IEnumerable<ExchangeBar> GetAggregatedCandlesByContract(int contractId, Instant from, Instant to,
        Period timeframe, string timezone)
        => _providers[_contractsToStreams[contractId]].GetBAUsByContract(contractId, from, to);

    public IEnumerable<ExchangeBar> GetAggregatedBausByStream(int streamId, Instant from, Instant to, Period timeframe,
        string timezone)
        => _providers[streamId].GetAggregatedBausByStream(streamId, from, to, timeframe, timezone);
}