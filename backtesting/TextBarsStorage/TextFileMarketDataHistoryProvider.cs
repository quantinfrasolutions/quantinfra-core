using System.Globalization;
using NodaTime;
using NodaTime.Text;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Backtesting.TextBarsStorage;

public class TextFileMarketDataHistoryProvider :
    IMarketDataHistoryProvider
{
    private readonly string _path;
    private readonly int _streamId;
    private readonly int? _contractId;
    private readonly Duration _tf;
    private readonly DateTimeZone _tz;
    private readonly Dictionary<int, IReadOnlyCollection<TradingSession>>? _tradingSessions;
    private readonly string _dateTimeFormat;
    private readonly Dictionary<int, DateTimeZone>? _timeZones;

    public TextFileMarketDataHistoryProvider(string path, int streamId, int? contractId, Duration? tf = null, DateTimeZone? tz = null, 
        Dictionary<int, IReadOnlyCollection<TradingSession>>? tradingSessions = null, string dateTimeFormat = "uuuu'-'MM'-'dd' 'HH':'mm':'ss")
    {
        _path = path;
        _streamId = streamId;
        _contractId = contractId;
        _tf = tf ?? Duration.FromMinutes(1);
        _tz = tz ?? DateTimeZoneProviders.Tzdb["UTC"];
        _tradingSessions = tradingSessions;
        _dateTimeFormat = dateTimeFormat;
    }


    public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to)
    {
        if (contractId != _contractId) throw new InvalidOperationException("ContractId doesn't match the ContractId of the storage");
        
        var storage = _tradingSessions != null ? 
            new StreamBarsStorage(new StreamReader(_path), _streamId, _contractId, _tradingSessions, dateTimeFormat: LocalDateTimePattern.Create(_dateTimeFormat, CultureInfo.InvariantCulture)) :
            new StreamBarsStorage(new StreamReader(_path), _streamId, _contractId, dateTimeFormat: LocalDateTimePattern.Create(_dateTimeFormat, CultureInfo.InvariantCulture));
        List<ExchangeBar> bars = new List<ExchangeBar>((int)((to - from).TotalMinutes + 1));
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
        if (streamId != _streamId) throw new InvalidOperationException("StreamId doesn't match the StreamId of the storage");
        
        var storage = _tradingSessions != null ? 
            new StreamBarsStorage(new StreamReader(_path), _streamId, _contractId, _tradingSessions) :
            new StreamBarsStorage(new StreamReader(_path), _streamId, _contractId);
        List<ExchangeBar> bars = new List<ExchangeBar>((int)((to - from).TotalMinutes + 1));
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