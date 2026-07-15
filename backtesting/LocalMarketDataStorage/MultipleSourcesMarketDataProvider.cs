using NodaTime;
using NodaTime.Text;
using QuantInfra.Backtesting.ParquetBarsStorage;
using QuantInfra.Backtesting.TextBarsStorage;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Backtesting.LocalMarketDataStorage;

public class MultipleSourcesMarketDataProvider : IMarketDataHistoryProvider
{
    private readonly string _dateTimeFormat;
    private readonly IReadOnlyDictionary<int, IMarketDataHistoryProvider> _providers;
    private readonly Dictionary<int, int> _contractsToStreams;
    
    public MultipleSourcesMarketDataProvider(
        IReadOnlyCollection<RequiredMarketDataUnitWithPath> units,
        string dateTimeFormat,
        IReadOnlyDictionary<int, IReadOnlyCollection<TradingSession>>? contractsTradingSessions = null
    )
    {
        _dateTimeFormat = dateTimeFormat;
        _providers = units
            .Where(u => u is { IsOk: true, StreamId: not null, DataRequired: true })
            .Select(u => new
                {
                    StreamId = u.StreamId.Value,
                    Provider = GetProvider(
                        u.Path,
                        u.StreamId!.Value,
                        u.ContractId,
                        u.ContractId.HasValue ? contractsTradingSessions?.GetValueOrDefault(u.ContractId.Value) : null
                    )
                }
            )
            .ToDictionary(p => p.StreamId, p => p.Provider);
        
        _contractsToStreams = units.Where(u => u is { IsOk: true, StreamId: not null, ContractId: not null })
            .ToDictionary(u => u.ContractId!.Value, u => u.StreamId!.Value);
    }

    private IMarketDataHistoryProvider GetProvider(
        string path, 
        int streamId, 
        int? contractId, 
        IReadOnlyCollection<TradingSession>? tradingSessions
    )
    {
        var tz = tradingSessions?.Select(ts => ts.Exchange.Timezone).Distinct().SingleOrDefault() 
            ?? DateTimeZoneProviders.Tzdb["UTC"];
        var tradingSessionsDict = contractId.HasValue && tradingSessions is not null
            ? new Dictionary<int, IReadOnlyCollection<TradingSession>> { { contractId.Value, tradingSessions } }
            : new Dictionary<int, IReadOnlyCollection<TradingSession>>();
        
        var extension = Path.GetExtension(path);
        var fileName = Path.GetFileNameWithoutExtension(path).Split('-');
        
        return extension switch
        {
            ".csv" or ".txt" => new TextFileMarketDataHistoryProvider(path, streamId, contractId, 
                PeriodPattern.Roundtrip.Parse(fileName[1]).Value.ToDuration(), 
                DateTimeZoneProviders.Tzdb[fileName[2].Replace(':', '/')],
                tradingSessionsDict, dateTimeFormat: _dateTimeFormat),
            ".parquet" => new ParquetFileMarketDataHistoryProvider(path, streamId, contractId,
                PeriodPattern.Roundtrip.Parse(fileName[1]).Value.ToDuration(),
                tradingSessionsDict),
            _ => throw new ArgumentException($"Unsupported file extension: {extension}", nameof(path))
        };
    }

    public IReadOnlyDictionary<long, double> GetLastKnownPrices(IEnumerable<long> contractIds, Instant dt)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyDictionary<int, double> GetLastKnownPrices(IEnumerable<int> contractIds, Instant dt)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ExchangeBar> GetBAUsByStream(int streamId, Instant from, Instant to)
    {
        if (!_providers.TryGetValue(streamId, out var provider))
            throw new KeyNotFoundException(streamId.ToString());
        
        return provider.GetBAUsByStream(streamId, from, to);
    }

    public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to)
    {
        if (!_contractsToStreams.TryGetValue(contractId, out var streamId)
            || !_providers.TryGetValue(streamId, out var provider)
           ) throw new KeyNotFoundException(contractId.ToString());
        
        return provider.GetBAUsByContract(contractId, from, to);
    }

    public IEnumerable<ExchangeBar> GetAggregatedCandlesByContract(int contractId, Instant from, Instant to,
        Period timeframe, string timezone)
    {
        if (!_contractsToStreams.TryGetValue(contractId, out var streamId)
            || !_providers.TryGetValue(streamId, out var provider)
        ) throw new KeyNotFoundException(contractId.ToString());
        
        return provider.GetAggregatedCandlesByContract(contractId, from, to, timeframe, timezone);
    }

    public IEnumerable<ExchangeBar> GetAggregatedBausByStream(int streamId, Instant from, Instant to, Period timeframe,
        string timezone)
    {
        if (!_providers.TryGetValue(streamId, out var provider))
            throw new KeyNotFoundException(streamId.ToString());
        
        return provider.GetAggregatedBausByStream(streamId, from, to, timeframe, timezone);
    }
}