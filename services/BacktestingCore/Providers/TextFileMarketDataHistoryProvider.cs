using System.Collections.Generic;
using System.IO;
using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Services.BacktestingCore.Providers;

public class TextFileMarketDataHistoryProvider :
    IMarketDataHistoryProvider
{
    private readonly string _path;
    private readonly int _streamId;
    private readonly int? _contractId;
    private readonly Duration _tf;
    private readonly DateTimeZone _tz;
    private readonly Dictionary<int, IReadOnlyCollection<TradingSession>>? _tradingSessions;
    private readonly Dictionary<int, DateTimeZone>? _timeZones;

    public TextFileMarketDataHistoryProvider(string path, int streamId, int? contractId, Duration? tf = null, DateTimeZone? tz = null, 
        Dictionary<int, IReadOnlyCollection<TradingSession>>? tradingSessions = null)
    {
        _path = path;
        _streamId = streamId;
        _contractId = contractId;
        _tf = tf ?? Duration.FromMinutes(1);
        _tz = tz ?? DateTimeZoneProviders.Tzdb["UTC"];
        _tradingSessions = tradingSessions;
    }


    public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to)
    {
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
        throw new System.NotImplementedException();
    }
}