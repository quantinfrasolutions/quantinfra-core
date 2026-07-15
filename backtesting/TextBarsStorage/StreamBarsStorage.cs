using System.Globalization;
using NodaTime;
using NodaTime.Text;
using QuantInfra.Common.MarketData;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Backtesting.TextBarsStorage;

public class StreamBarsStorage
{
    private readonly LocalDateTimePattern _dateTimeFormat;
    private readonly Dictionary<int, StreamReader> _readers;
    private readonly Dictionary<int, int?> _streamsToContracts;
    
    private readonly char _separator;
    private readonly int _dtColNum,  _openColNum, _highColNum, _lowColNum, _closeColNum, _volColNum;
    
    private TradingSessionWatcher<int>? _tsw = null;
    private readonly Duration _oneMinuteDuration = Duration.FromMinutes(1);

    public StreamBarsStorage(StreamReader reader,
        int streamId,
        int? contractId,
        LocalDateTimePattern? dateTimeFormat = null,
        char separator = ',',
        int dtColNum = 0,
        int openColNum = 1,
        int highColNum = 2,
        int lowColNum = 3,
        int closeColNum = 4,
        int volColNum = 5
    )
    {
        _dateTimeFormat = dateTimeFormat ?? LocalDateTimePattern.ExtendedIso;
        _readers = new() { { streamId, reader } };
        _streamsToContracts = new() { { streamId, contractId } };
        _separator = separator;
        _dtColNum = dtColNum;
        _openColNum = openColNum;
        _highColNum = highColNum;
        _lowColNum = lowColNum;
        _closeColNum = closeColNum;
        _volColNum = volColNum;
    }

    public StreamBarsStorage(
        StreamReader reader,
        int streamId,
        int? contractId,
        Dictionary<int, IReadOnlyCollection<TradingSession>> tradingSessions,
        LocalDateTimePattern? dateTimeFormat = null
    ) : this (reader, streamId, contractId, dateTimeFormat)
    {
        _tsw = new TradingSessionWatcher<int>(
                tradingSessions.ToDictionary(ts => ts.Key, ts => ts.Value)
            );
    }

    public StreamBarsStorage(Dictionary<int, StreamReader> readers, Dictionary<int, int?> streamsToContracts, LocalDateTimePattern? dateTimeFormat = null)
    {            
        _readers = readers;            
        _streamsToContracts = streamsToContracts;
        _dateTimeFormat = dateTimeFormat ?? LocalDateTimePattern.ExtendedIso;
    }

    public StreamBarsStorage(
        Dictionary<int, StreamReader> readers,
        Dictionary<int, int?> streamsToContracts,
        Dictionary<int, IReadOnlyCollection<TradingSession>> tradingSessions,
        LocalDateTimePattern? dateTimeFormat = null
    ) : this(readers, streamsToContracts, dateTimeFormat)
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

    public ExchangeBar Read(int streamId, Duration? tf = null, DateTimeZone? tz = null)
    {
        tf ??= Duration.FromMinutes(1);
        tz ??= DateTimeZoneProviders.Tzdb["UTC"];
        
        var line = _readers[streamId].ReadLine()!.Split(_separator);
        var contractId = _streamsToContracts[streamId];
        
        var dt = _dateTimeFormat.Parse(line[_dtColNum]).Value
            .InZoneStrictly(tz).ToInstant();
        var (tsId, closeDt) = ApplyDtAndGetTradingSessionIdAndCloseDt(streamId, tf.Value, dt);
        return new ExchangeBar(streamId, contractId, dt, closeDt,
            double.Parse(line[_openColNum], CultureInfo.InvariantCulture),
            double.Parse(line[_highColNum], CultureInfo.InvariantCulture),
            double.Parse(line[_lowColNum], CultureInfo.InvariantCulture),
            double.Parse(line[_closeColNum], CultureInfo.InvariantCulture),
            double.Parse(line[_volColNum], CultureInfo.InvariantCulture),
            0,
            tsId
        );
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