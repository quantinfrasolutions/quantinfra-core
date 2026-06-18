using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.MarketData;

/// <summary>
/// Keeps track of the current trading session for multiple entities (contracts or streams)
/// </summary>
/// <typeparam name="TId"></typeparam>
public class TradingSessionWatcher<TId>
{
    private readonly IReadOnlyDictionary<TId, List<TradingSession>> _entities;
    private readonly IReadOnlyDictionary<TId, DateTimeZone> _timezones;
    private readonly IReadOnlyDictionary<TId, IReadOnlyList<TradingScheduleInterval>> _schedules;

    private readonly Dictionary<TId, Instant> _tradingSessionsEndDts;
    private readonly Dictionary<TId, TradingSession?> _currentTradingSessions;
        
    /// <param name="tradingSessions">Dictionary of trading sessions by entity id</param>
    public TradingSessionWatcher(
        IReadOnlyDictionary<TId, IReadOnlyCollection<TradingSession>> tradingSessions
    )
    {
        _entities = tradingSessions.ToDictionary(
            i => i.Key,
            i => i.Value?.ToList() ?? new List<TradingSession>()
        );
        _schedules = tradingSessions.ToDictionary(
            i => i.Key,
            i => i.Value?.ToList().GetSchedule() ?? new List<TradingSession>().GetSchedule()
        );
        _tradingSessionsEndDts = tradingSessions.ToDictionary(
            i => i.Key,
            i => Instant.MinValue
        );
        _currentTradingSessions = new Dictionary<TId, TradingSession?>();
        _timezones = tradingSessions.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Select(ts => ts.Exchange.Timezone).Distinct().SingleOrDefault() ?? DateTimeZone.Utc
        );
    }
        

    public static (LocalDateTime, TradingSession?, TradingSessionInterval?) GetCurrentTradingSession(IEnumerable<TradingSession> tradingSessions, DateTimeZone tz, Instant dt)
    {
        var localDt = dt.InZone(tz).LocalDateTime;
        var dow = localDt.DayOfWeek;
        var time = localDt.TimeOfDay;
        var res = tradingSessions
            .Select(ts => new
            {
                ts, 
                Interval = ts.Days.SingleOrDefault(d => 
                    d.StartDay <= dow && d.Start <= time 
                                      && d.EndDay >= dow && (d.End == LocalTime.Midnight || d.End > time)
                )
            })
            .SingleOrDefault(x => x.Interval != null);
        return (localDt, res?.ts, res?.Interval);
    }

    public static (LocalDateTime, TradingScheduleInterval?) GetCurrentTradingSession(
        IEnumerable<TradingScheduleInterval> schedule, DateTimeZone tz, Instant dt)
    {
        var localDt = dt.InZone(tz).LocalDateTime;
        var dayTime = new DayTime(localDt.DayOfWeek, localDt.TimeOfDay);
        return (localDt, schedule.GetTradingSession(dayTime));
    }

    public TradingSession? ProcessUpdateAndGetCurrentSessionId(TId id, Instant dt)
    {
        if (_schedules.TryGetValue(id, out var schedule) && schedule.Count > 0)
        {
            if (dt >= _tradingSessionsEndDts[id])
            {
                var (localDt, interval) = GetCurrentTradingSession(schedule, _timezones[id], dt);

                _currentTradingSessions[id] = interval?.TradingSession;
                if (interval is not null)
                {
                    _tradingSessionsEndDts[id] = TradingSessionExtensions.GetInstant(localDt, interval.End, _timezones[id]);
                }
                else
                {
                    _tradingSessionsEndDts[id] = Instant.MinValue;
                }
            }
            return _currentTradingSessions[id];
        }
        // if (_entities.TryGetValue(id, out var entity) && entity.Count > 0)
        // {                
        //     if (dt >= _tradingSessionsEndDts[id])
        //     {
        //         var (localDt, session, day) = GetCurrentTradingSession(entity, _timezones[id], dt);
        //
        //         _currentTradingSessions[id] = session;
        //         if (session != null)
        //         {
        //             LocalDateTime sessionEndLocal;
        //
        //             if (session.Is24X7)
        //             {
        //                 sessionEndLocal =
        //                     new LocalDateTime(localDt.Year, localDt.Month, localDt.Day, 0, 0, 0)
        //                         .Plus(Period.FromDays(1));
        //             }
        //             else
        //             {
        //                 sessionEndLocal = new LocalDateTime(localDt.Year, localDt.Month, localDt.Day, day.End.Hour, day.End.Minute);
        //                 if (day.End == LocalTime.Midnight)
        //                     sessionEndLocal = sessionEndLocal.Plus(Period.FromDays(1));
        //             }
        //             var tz = _timezones[id];
        //             var sessionEndDt = sessionEndLocal
        //                 .InZoneLeniently(tz)
        //                 .ToInstant();
        //             _tradingSessionsEndDts[id] = sessionEndDt;
        //         }
        //         else
        //         {
        //             _tradingSessionsEndDts[id] = Instant.MinValue;
        //         }
        //     }
        //     return _currentTradingSessions[id];
        // }
        return null;
    }
}