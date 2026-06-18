using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Sdk.MarketData;

[assembly: InternalsVisibleTo("Tests")]

namespace QuantInfra.Common.MarketData;

public class BarStorage : IEnumerable<IBar>, IBarStorage// : MarketData.BarStorage
{
    CircularBuffer<Bar> _barsRaw;
    Bar[] _bars; // TODO: make it list?
    readonly ILoggerFactory _loggerFactory;
    BarStorageConfig _barStorageConfig;
    //Stopwatch sw = new Stopwatch();
    // protected IProfiler Profiler = Profiling.Profiler.ActiveProfiler;
    AbstractIndicator[] _indicators = new AbstractIndicator[0];
    readonly HashSet<string> _indicatorIds = new HashSet<string>();

    private AbstractIndicator[] _ohlcv = new AbstractIndicator[5];
    private readonly HashSet<string> _ohlcvIds = new(5);
    // readonly protected Dictionary<string, AbstractIndicator> _ohlcv = new Dictionary<string, AbstractIndicator>();
    protected ILogger Logger { get; set; }


    public BarStorage(ILoggerFactory loggerFactory, BarStorageConfig barStorageConfig, int streamId)
    {
        // _loggerFactory = loggerFactory;
        // Logger = _loggerFactory.CreateLogger($"BarStorage.{barStorageConfig.FullQualifier}");
        _barStorageConfig = barStorageConfig;
        StreamId = streamId;
        InitiateStorage(Capacity);
        EnsureOHLC();
    }


    public IEnumerator<IBar> GetEnumerator() => ((IEnumerable<Bar>)_bars).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _bars.GetEnumerator();
    public BarStorageConfig BarStorageConfig => _barStorageConfig;
    public int StreamId { get; }
    public int Capacity { get; private set; } = 0;
    public virtual int CapacityInBAU { get; }
    /// <summary>
    /// Indicates if the data has been loaded to the storage, so it's now in its full capacity
    /// </summary>
    public bool IsInitialized { get; private set; } = false;
    public Dictionary<string, IBarStorage> RequiredBarStorages { get; } = new();

    // public Bar this[int index] => _bars.ElementAt(index);
    public IBar this[int index] => _bars[index];
    public int Count => _barsRaw.Count;
    // public Bar? CurrentBar => Count > 0 ? this[0] : null;
    public IBar? CurrentBar { get; private set; } = null;
    //public Bar? CurrentBar => _bars.Count > 0 ? _bars.Front() : null;
    /// <summary>
    /// Indicator qualifier => Indicator
    /// </summary>
    public Dictionary<string, AbstractIndicator> GetIndicators() => _ohlcv.Union(_indicators).ToDictionary(i => i.Id, i => i);

    public string FullQualifier => _barStorageConfig.FullQualifier;

    public virtual Instant? AppendBar(ExchangeBar bar)
    {
#if PROFILE
            using (Profiler.Step("BarStorage.AppendBar.Preparations"))
            {
#endif
        // in case we're processing messages from buffer received while we were getting the history
        if (CurrentBar != null && bar.OpenDt <= CurrentBar.OpenDt) return null;

        _barsRaw.PushFront(new Bar(_indicators.Length) { OpenDt = bar.OpenDt, CloseDt = bar.CloseDt });
        _bars = _barsRaw.ToArray(); // TODO: performance — try to change to List?
        CurrentBar = _bars[0];
        if (_bars.Length >= Capacity) IsInitialized = true;
        //_bars.Push(new Bar { OpenDt = bar.OpenDt, CloseDt = bar.CloseDt });                
        //CurrentBar!.Closed = true;
#if PROFILE
            }
#endif
#if PROFILE
            using (Profiler.Step("BarStorage.AppendBar.CalculateIndicators"))
            {
            using (Profiler.Step("OHLCV"))
            {
#endif
        _ohlcv[0].CalculateIndicator(this, bar.Open);
        _ohlcv[1].CalculateIndicator(this, bar.High);
        _ohlcv[2].CalculateIndicator(this, bar.Low);
        _ohlcv[3].CalculateIndicator(this, bar.Close);
        _ohlcv[4].CalculateIndicator(this, bar.Volume);
#if PROFILE
            }
#endif
        foreach (var i in _indicators)
        {
            if (_barsRaw.Count > i.SkipBars)
            {
#if PROFILE
                    using (Profiler.Step($"{i.GetType().Name}"))
#endif
                i.CalculateIndicator(this);
            }
        }
#if PROFILE
            }
#endif
#if !FAST
        // Logger.LogTrace($"{bar.CloseDt}|{{{string.Join(',', _indicators.Select(i => i.GetLogInformation(CurrentBar)))}}}");
#endif
        return null;
    }

    public void AppendBars(IEnumerable<ExchangeBar> bars)
    {
        foreach (var b in bars)
        {
            AppendBar(b);
        }
    }

    public int RegisterIndicator(AbstractIndicator indicator, int lookback = 0)
    {
        var totalRequiredBars = UnwindAndRegisterIndicators(new NestedIndicator(indicator, lookback));
        if (totalRequiredBars > Capacity || _barsRaw == null)
        {
            InitiateStorage(totalRequiredBars);
        }
        return totalRequiredBars;
    }        

    int UnwindAndRegisterIndicators(NestedIndicator indicator)
    {
        var childrenSkipBars = 0;
        foreach (var i in indicator.Indicator.GetNestedIndicators())
        {
            childrenSkipBars = Math.Max(childrenSkipBars, UnwindAndRegisterIndicators(i) - 1);
        }
        indicator.Indicator.SkipBars = childrenSkipBars;
        indicator.Indicator.ReadyAt = childrenSkipBars + indicator.Indicator.WarmupBars + 1;
        if (!_ohlcvIds.Contains(indicator.Indicator.Id))
        {
            if (_indicatorIds.Add(indicator.Indicator.Id))
            {
                var indicators = new AbstractIndicator[_indicators.Length + 1];
                _indicators.CopyTo(indicators, 0);
                indicators[^1] = indicator.Indicator;
                _indicators = indicators;
            }
        }
        return indicator.Indicator.ReadyAt + indicator.Lookback;
    }

    public virtual int GetBarsCountSinceMoment(Instant startDate)
    {
        throw new NotImplementedException();
    }
        
    public void SetInitialized()
    {
        IsInitialized = true;
    }
        
    public void Clear()
    {
        IsInitialized = false;
        _barsRaw.Clear();
    }

    private void EnsureOHLC()
    {
        _ohlcv[0] = new Open();
        _ohlcvIds.Add(_ohlcv[0].Id);
        _ohlcv[1] = new High();
        _ohlcvIds.Add(_ohlcv[1].Id);
        _ohlcv[2] = new Low();
        _ohlcvIds.Add(_ohlcv[2].Id);
        _ohlcv[3] = new Close();
        _ohlcvIds.Add(_ohlcv[3].Id);
        _ohlcv[4] = new Volume();
        _ohlcvIds.Add(_ohlcv[4].Id);
    }

    public void InitiateStorage(int capacity)
    {
        if (capacity <= Capacity && capacity > 1) return;
        Capacity = Math.Max(capacity, 1);
        IsInitialized = false;
        _barsRaw = new CircularBuffer<Bar>(Capacity);

        foreach (var bs in RequiredBarStorages.Values)
        {
            bs.InitiateStorage(capacity);
        }
    }
        
    protected virtual void CopyFrom(BarStorage other)
    {
        if (_barsRaw.Count != 0)
            throw new Exception("Cannot copy to a non-empty bar storage");

        if (other._bars.Length < Capacity)
            throw new Exception("Not enough capacity to copy");
            
        foreach (var b in other._bars.Reverse())
        {
            _barsRaw.PushFront(b);
        }
        _bars = _barsRaw.ToArray();
        CurrentBar = _bars[0];
        IsInitialized = true;
    }
}