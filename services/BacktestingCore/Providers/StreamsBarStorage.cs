using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NodaTime;
using QuantInfra.Common.MarketData;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Services.BacktestingCore.Providers;

public class StreamBarsStorage
{
    private readonly string _dateTimeFormat;
    private readonly Dictionary<int, StreamReader> _readers;
    private readonly Dictionary<int, int?> _streamsToContracts;
    
    private TradingSessionWatcher<int>? _tsw = null;
    private readonly Duration _oneMinuteDuration = Duration.FromMinutes(1);

    public StreamBarsStorage(StreamReader reader, int streamId, int? contractId, string dateTimeFormat = "o")
    {
        _dateTimeFormat = dateTimeFormat;
        _readers = new() { { streamId, reader } };
        _streamsToContracts = new() { { streamId, contractId } };
    }

    public StreamBarsStorage(
        StreamReader reader,
        int streamId,
        int? contractId,
        Dictionary<int, IReadOnlyCollection<TradingSession>> tradingSessions,
        string dateTimeFormat = "o"
    ) : this (reader, streamId, contractId, dateTimeFormat)
    {
        _tsw = new TradingSessionWatcher<int>(
                tradingSessions.ToDictionary(ts => ts.Key, ts => ts.Value)
            );
    }

    public StreamBarsStorage(Dictionary<int, StreamReader> readers, Dictionary<int, int?> streamsToContracts, string dateTimeFormat = "o")
    {            
        _readers = readers;            
        _streamsToContracts = streamsToContracts;
        _dateTimeFormat = dateTimeFormat;
    }

    public StreamBarsStorage(
        Dictionary<int, StreamReader> readers,
        Dictionary<int, int?> streamsToContracts,
        Dictionary<int, IReadOnlyCollection<TradingSession>> tradingSessions,
        string dateTimeFormat = "o"
    ) : this(readers, streamsToContracts)
    {
        _tsw = new TradingSessionWatcher<int>(
                tradingSessions.ToDictionary(ts => ts.Key, ts => ts.Value)
            );
    }

    public List<ExchangeBar> Read(Duration? tf = null, DateTimeZone? tz = null)
    {
        tf ??= Duration.FromMinutes(1);
        tz ??= DateTimeZoneProviders.Tzdb["UTC"];
        if (CanRead)
        {
            return _readers
                .Keys
                .Select(r => Read(r, tf, tz))
                .ToList();
        }
        else return null;
    }

    public bool CanRead => !_readers.Values.Any(r => r.EndOfStream);

    ExchangeBar Read(int streamId, Duration? tf = null, DateTimeZone? tz = null)
    {
        tf ??= Duration.FromMinutes(1);
        tz ??= DateTimeZoneProviders.Tzdb["UTC"];
        
        var line = _readers[streamId].ReadLine().Split(',');
        var contractId = _streamsToContracts[streamId];

        var dateString = line[0];
        var timeString = line[1];
        if (dateString.Contains('-'))
        {
            var dt = new LocalDateTime(
                Convert.ToInt32(dateString.Substring(0, 4)),
                Convert.ToInt32(dateString.Substring(5, 2)),
                Convert.ToInt32(dateString.Substring(8, 2)),
                Convert.ToInt32(dateString.Substring(11, 2)),
                Convert.ToInt32(dateString.Substring(14, 2))
            ).InZoneStrictly(tz).ToInstant();
            var (tsId, closeDt) = ApplyDtAndGetTradingSessionIdAndCloseDt(streamId, tf.Value, dt);
            return new ExchangeBar(streamId, contractId, dt, closeDt,
                double.Parse(line[1], CultureInfo.InvariantCulture),
                double.Parse(line[2], CultureInfo.InvariantCulture),
                double.Parse(line[3], CultureInfo.InvariantCulture),
                double.Parse(line[4], CultureInfo.InvariantCulture),
                double.Parse(line[5], CultureInfo.InvariantCulture),
                0,
                tsId
            );
        }
        else
        {
            var dt = new LocalDateTime(
                Convert.ToInt32(dateString.Substring(0, 4)),
                Convert.ToInt32(dateString.Substring(4, 2)),
                Convert.ToInt32(dateString.Substring(6, 2)),
                Convert.ToInt32(timeString.Substring(0, 2)),
                Convert.ToInt32(timeString.Substring(2, 2)),
                Convert.ToInt32(timeString.Substring(4, 2))
            ).InZoneStrictly(tz).ToInstant();
            var (tsId, closeDt) = ApplyDtAndGetTradingSessionIdAndCloseDt(streamId, tf.Value, dt);
            return new ExchangeBar
            {
                StreamId = streamId,   
                ContractId = contractId,
                OpenDt = dt,
                CloseDt = dt.Plus(tf!.Value),
                Open = double.Parse(line[2], CultureInfo.InvariantCulture),
                High = double.Parse(line[3], CultureInfo.InvariantCulture),
                Low = double.Parse(line[4], CultureInfo.InvariantCulture),
                Close = double.Parse(line[5], CultureInfo.InvariantCulture),
                Volume = double.Parse(line[6], CultureInfo.InvariantCulture),
                TradingSessionId = _tsw?.ProcessUpdateAndGetCurrentSessionId(streamId, dt)?.TradingSessionId
            };
        }
    }

    private (int?, Instant) ApplyDtAndGetTradingSessionIdAndCloseDt(int streamId, Duration tf, Instant openDt)
    {
        var closeDt = openDt.Plus(tf);
        var tsId = _tsw?.ProcessUpdateAndGetCurrentSessionId(streamId, openDt)?.TradingSessionId;
        if (tf > _oneMinuteDuration)
        {
            // bars that are longer than 1 minute may have a boundary of two trading sessions inside
            // in this case chose the latest trading session
            tsId = _tsw?.ProcessUpdateAndGetCurrentSessionId(streamId, closeDt)?.TradingSessionId ?? tsId;
        }

        return (tsId, closeDt);
    }
}