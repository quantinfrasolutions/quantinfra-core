using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;

[assembly:InternalsVisibleTo("Tests.Unit.Common.MarketData")]
namespace QuantInfra.Common.MarketData
{
    public sealed class AggregatingBarStorage : BarStorage, IAggregatingBarStorage
    {                
        private AggregatingBar? _aggregation;

        private readonly Duration _timeframe;
        private readonly Duration _offset;
        private readonly int _bauCapacityMultiplier;
        private readonly DateTimeZone _tz, _exchangeTz;
        private readonly IReadOnlyList<TradingScheduleInterval>? _tradingSchedule;
        
        private LocalTime _dayStart;
        Instant _lastProcessedBarOpenDt = Instant.MinValue;
        Instant _tradingSessionClose;

        public AggregatingBarStorage(
            ILoggerFactory loggerFactory,
            IReadOnlyCollection<TradingSession> tradingSessions,
            BarStorageConfig barStorageConfig,
            int streamId
        ) : base(loggerFactory, barStorageConfig, streamId)
        {
            _timeframe = barStorageConfig.Timeframe.ToDuration();
           
            _offset = barStorageConfig.Offset?.ToDuration() ?? Duration.Zero;
            if (_offset.TotalMilliseconds < 0) throw new ArgumentException("Offset should be non-negative");
            
            _dayStart = new LocalTime(_offset.Hours, _offset.Minutes);

            if (_timeframe > Duration.FromDays(1))
            {
                throw new NotImplementedException("Timeframes greater than 1 day are not yet supported");
            }
            if (_offset != null && _offset >= _timeframe)
            {
                throw new ArgumentException("Offset should be less than timeframe");
            }
            var timeframeMinutes = barStorageConfig.Timeframe.ToDuration().TotalMinutes;            
            if (24 * 60 % timeframeMinutes != 0) throw new ArgumentException("Timeframe must fit into 24 hours");            
            Timeframe = barStorageConfig.Timeframe;
            Offset = barStorageConfig.Offset;

            _bauCapacityMultiplier = (int)Math.Ceiling(timeframeMinutes);
            
            _tz = DateTimeZoneProviders.Tzdb[barStorageConfig.Timezone];
            
            TradingSessions = barStorageConfig.TradingSessionIds?
                .ToDictionary(
                    i => i,
                    i => tradingSessions.Single(ts => ts.TradingSessionId == i)
                );

            _exchangeTz = TradingSessions?.Count > 0 ? TradingSessions.Values.Select(ts => ts.Exchange.Timezone).Distinct().Single() : _tz;

            _tradingSchedule = TradingSessions?.Values.GetSchedule();
            //FillCloseDts();
        }


        public Period Timeframe { get; }
        public Period? Offset { get; }
        
        public Dictionary<int, TradingSession>? TradingSessions { get; }

        public double GetBarsRequestReserveFactor()
        {
            if (TradingSessions == null || TradingSessions.Count == 0 || Timeframe == Period.FromDays(1)) return 7.0 / 5;

            var totalDuration = Duration.Zero;
            foreach (var ts in TradingSessions.Values)
            {
                if (ts.Is24X7) return 1;

                var tsDuration = _tradingSchedule?.GetTradingMinutesPerWeek() ?? 0;
                return tsDuration == 0 ? 0 : (24 * 60 * 7) / tsDuration;
                // foreach (var d in ts.Days)
                // {
                //     totalDuration = totalDuration.Plus((d.End - d.Start).ToDuration());
                //     if (d.End == LocalTime.Midnight) totalDuration = totalDuration.Plus(Duration.FromDays(1));
                // }
            }

            var res = Duration.FromDays(7) / totalDuration;
            return res;
        }

        public ExchangeBar? CurrentAggregation => _aggregation?.ToExchangeBar();


        public override Instant? AppendBar(ExchangeBar bar) => AppendBarInternal(bar, 0);

        private Instant? AppendBarInternal(ExchangeBar bar, int callNumber)
        {
            if (callNumber > 1)
            {
                // Not checking this leads to StackOverflowException
                throw new Exception($"AppendBarInternal was called recursively more than once. Usually, this means a mistake in trading sessions configuration. BarOpenDt={bar.OpenDt}");
            }

            var barOpenDt = bar.OpenDt;
            
            // if (barOpenDt == Instant.FromUtc(2016, 03, 14, 00, 00)) Debugger.Break();
            
            // TODO: add several bars in case GetBarOpeningDt(bar.OpenDt) - _aggregation.OpenDt > 1 x Timeframe
            if (callNumber == 0 && barOpenDt <= _lastProcessedBarOpenDt)
                return null;

            _lastProcessedBarOpenDt = barOpenDt;
            
            // if (Timeframe == Period.FromDays(1) && !BarStorageConfig.LastValueOnly && bar.TradingSessionId.HasValue) Debugger.Break();
            
            if (_aggregation == null)
            {
                // if the config limits trading sessions
                if (TradingSessions != null && TradingSessions.Any())
                {
                    // do not append bars that are not for the configured sessions
                    if (!bar.TradingSessionId.HasValue || !TradingSessions.ContainsKey(bar.TradingSessionId.Value))
                    {                        
                        return null;
                    }
                }
                
                var (barOpeningDt, dow, localDt) = GetBarOpeningDt(barOpenDt);
                // TODO
                // var tradingSessions = TradingSessions?
                //     .Values
                //     .Where(ts => ts.Days.ContainsKey(dow))
                //     .OrderBy(ts => ts.Days[dow].Start)
                //     .ToList() ?? new List<TradingSession>();
                
                
                var barClosingTs = barOpeningDt.Plus(_timeframe);

                if (_tradingSchedule is not null)
                {
                    var barClosingLocal = barClosingTs.InZone(_exchangeTz).LocalDateTime;
                    var barClosingDt = new DayTime(barClosingLocal.DayOfWeek, barClosingLocal.TimeOfDay);

                    // If the trading session ends before the bar closing time, the closing time must be set to the trading session end.
                    // However, if there are multiple continuous trading sessions, the boundary is defined by the last trading session.
                    TradingScheduleInterval? lastInverval = null;
                    for (var i = 0; i < _tradingSchedule!.Count; i++)
                    {
                        var ts = _tradingSchedule[i];
                        if (ts.Start > barClosingDt) break;
                        if (ts.TradingSession is not null) lastInverval = ts;
                    }

                    if (lastInverval is not null)
                    {
                        _tradingSessionClose = TradingSessionExtensions.GetInstant(localDt, lastInverval.End,
                            lastInverval.TradingSession!.Exchange.Timezone);
                    }
                    else
                    {
                        _tradingSessionClose = barClosingTs;
                    }
                }
                else _tradingSessionClose = barClosingTs;

                _aggregation = new(bar.StreamId, bar.ContractId, barOpeningDt, Instant.Min(_tradingSessionClose, barClosingTs),
                    bar.Open, bar.High, bar.Low, bar.Close, 0, 0, bar.TradingSessionId);                
            }            
            
            if (
                barOpenDt > _aggregation.CloseDt
                || barOpenDt > _tradingSessionClose
                //|| bar.TradingSessionId != _aggregation.TradingSessionId
            )
            {
                var closeDt = CloseAggregation();
                return AppendBarInternal(bar, callNumber + 1) ?? closeDt; // If two bars closed at the same moment, return the dt of the last
            }

            _aggregation.High = Math.Max(bar.High, _aggregation.High);
            _aggregation.Low = Math.Min(bar.Low, _aggregation.Low);
            _aggregation.Close = bar.Close;
            _aggregation.Volume += bar.Volume;

            var barCloseDt = bar.CloseDt;
            if (barCloseDt == _aggregation.CloseDt || barCloseDt == _tradingSessionClose)
            {
                return CloseAggregation();
            }

            return null;
        }

        public override int CapacityInBAU => base.BarStorageConfig.LastValueOnly ? 1 : Capacity * _bauCapacityMultiplier;
        
        /// <returns>Universal time of the bar open time and the local day of week (in the time zone of the bar storage)</returns>
        public (Instant, IsoDayOfWeek, LocalDateTime) GetBarOpeningDt(Instant dt) =>
            GetBarOpeningDt(dt, _tz, _timeframe, _offset);
        
        public static (Instant, IsoDayOfWeek, LocalDateTime) GetBarOpeningDt(Instant dt, DateTimeZone tz, Duration tf, Duration offset)
        {
            var zonedDt = dt.InZone(tz);
            var dow = zonedDt.LocalDateTime.DayOfWeek;

            var utcLocalDt = dt.InUtc().LocalDateTime;
            var utcDayStart = Instant
                .FromUtc(utcLocalDt.Year, utcLocalDt.Month, utcLocalDt.Day, 0, 0, 0)
                .Minus(Duration.FromTicks(zonedDt.Offset.Ticks))
                .Minus(Duration.FromDays(1))
                .Plus(offset);

            // if (offset.TotalMilliseconds != 0) utcDayStart = utcDayStart.Minus(Duration.FromDays(1));

            var closedIntervalsCount = (int)((dt - utcDayStart) / tf);

            return (utcDayStart.Plus(tf * closedIntervalsCount), dow, zonedDt.LocalDateTime);
        }

        public Instant GetTradingSessionBoundary(Instant dt, LocalTime boundary, TradingSession ts)
        {
            var localDt = dt.InZone(ts.Exchange.Timezone).LocalDateTime;
            var localBoundary = new LocalDateTime(localDt.Year, localDt.Month, localDt.Day, boundary.Hour, boundary.Minute);
            if (boundary == LocalTime.Midnight) localBoundary = localBoundary.Plus(Period.FromDays(1));
            var res = localBoundary.InZoneLeniently(ts.Exchange.Timezone).ToInstant();
            return res;
        }

        // TODO: this works incorrectly because it doesn't account for non-working days and trading sessions
        public override int GetBarsCountSinceMoment(Instant startDate) =>
            (int)((CurrentBar.OpenDt - GetBarOpeningDt(startDate).Item1) / _timeframe) + 1; // (CurrentBar.Closed ? 1 : 0);


        private Instant CloseAggregation()
        {            
            base.AppendBar(_aggregation.ToExchangeBar());
            var closeDt = _aggregation.CloseDt;
            _aggregation = null;
            return closeDt;
        }

        public void CopyFrom(AggregatingBarStorage other)
        {
            base.CopyFrom(other);
            _aggregation = other._aggregation;
            _dayStart = other._dayStart;
            _lastProcessedBarOpenDt = other._lastProcessedBarOpenDt;
            _tradingSessionClose = other._tradingSessionClose;
        }
    }
}
