using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Backtesting.TextBarsStorage;

public class TextFileMultiHistoryProviderConfig
{
    public Dictionary<int, string> Streams { get; set; } = new();
    public Dictionary<int, int?> StreamsToContracts { get; set; } = new();
}

public class TextFileMultiMarketDataHistoryProvider :
    IMarketDataHistoryProvider
{
    private readonly TextFileMultiHistoryProviderConfig _config;
    private readonly Duration _tf;
    private readonly DateTimeZone _tz;
    private readonly Dictionary<int, IReadOnlyCollection<TradingSession>>? _tradingSessions;

    public TextFileMultiMarketDataHistoryProvider(TextFileMultiHistoryProviderConfig config, Duration? tf = null, DateTimeZone? tz = null, 
        Dictionary<int, IReadOnlyCollection<TradingSession>> tradingSessions = null)
    {
        _config = config;
        _tf = tf ?? Duration.FromMinutes(1);
        _tz = tz ?? DateTimeZoneProviders.Tzdb["UTC"];
        _tradingSessions = tradingSessions;
    }


    public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to)
    {
        var storage = _tradingSessions != null ? 
            new StreamBarsStorage(new StreamReader(_config.Streams[contractId]), contractId, contractId, _tradingSessions) :
            new StreamBarsStorage(new StreamReader(_config.Streams[contractId]), contractId, contractId);
        List<ExchangeBar> bars = new List<ExchangeBar>();
        while (storage.CanRead)
        {
            var bar = storage.Read(_tf, _tz)[0];
            if (bar.OpenDt >= from && bar.CloseDt <= to)
            {
                bars.Add(bar);
            }
            if (bar.CloseDt > to) break;
        }
        return bars;
    }

    // TODO: aggregation is currently not supported for the provider
    
    public IEnumerable<ExchangeBar> GetAggregatedCandlesByContract(int contractId, Instant from, Instant to,
        Period timeframe, string timezone) =>
        GetBAUsByContract(contractId, from, to);

    public IEnumerable<ExchangeBar> GetAggregatedBausByStream(int streamId, Instant from, Instant to, Period timeframe,
        string timezone) =>
        GetBAUsByStream(streamId, from, to);

    public IReadOnlyDictionary<int, double> GetLastKnownPrices(IEnumerable<int> contractIds, Instant dt)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<ExchangeBar> GetBAUsByStream(int streamId, Instant from, Instant to)
    {
        var storage = new StreamBarsStorage(new StreamReader(_config.Streams[streamId]), streamId, 
            _config.StreamsToContracts.GetValueOrDefault(streamId));
        
        List<ExchangeBar> bars = new List<ExchangeBar>();
        while (storage.CanRead)
        {
            var bar = storage.Read(_tf, _tz)[0];
            if (bar.OpenDt >= from && bar.CloseDt <= to)
            {
                bars.Add(bar);
            }
            if (bar.CloseDt > to) break;
        }
        return bars;
    }
}