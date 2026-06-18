// using System;
// using System.Collections.Generic;
// using System.Linq;
// using BacktestingCore.Providers;
// using Common.Backtesting;
// using Common.MarketData;
// using Microsoft.Extensions.Logging;
// using NodaTime;
//
// namespace StrategyTester.Providers
// {
//     public class InMemoryCandlesStorage :
//         ITestMarketDataProvider
// 	{
//         readonly IMarketDataHistoryProvider _historyProvider;
//         readonly Instant _fromDt;
//         readonly Instant _endDt;
//
//         List<ExchangeBar> _bars;
//
//         bool _barsLoaded;        
//
//         Instant? _historicalBarsRequestedFrom = null;
//         Instant _currentInstant;
//         int _currentPosition;
//         private int _historicalBarsCount;
//
// 		public InMemoryCandlesStorage(
//             IMarketDataHistoryProvider historyProvider,
//             ILogger<InMemoryCandlesStorage> logger,
//             InMemoryCandlesStorageOptions options
//         )
// 		{
//             _historyProvider = historyProvider;
//             _fromDt = options.StartDt;
//             _endDt = options.EndDt;
//             _currentInstant = options.StartDt;
//         }        
//
//         public bool CanRead => _currentPosition < _bars.Count;
//
//         public ExchangeBar GetNextBar()
//         {
//             _currentInstant = _bars[_currentPosition].CloseDt;
//             return _bars[_currentPosition++];
//         }
//
//         public void Restart()
//         {
//             throw new NotImplementedException();
//         }
//
//         public Instant GetCurrentInstant() => _currentInstant;
//
//         public Dictionary<long, double> GetLastKnownPrices(IEnumerable<long> contractIds, Instant dt)
//         {
//             throw new NotImplementedException();
//         }
//
//         public IEnumerable<ExchangeBar> GetBAUsByStream(long streamId, Instant from, Instant to, BarAggregationType aggType = BarAggregationType.Time)
//         {
//             throw new NotImplementedException();
//         }
//
//         public IEnumerable<ExchangeBar> GetBAUsByStream(long streamId, Instant to, int numberOfBAUs, BarAggregationType aggType = BarAggregationType.Time)
//         {
//             throw new NotImplementedException();
//         }
//
//         public IEnumerable<ExchangeBar> GetBAUsByContract(long contractId, Instant from, Instant to, BarAggregationType aggType = BarAggregationType.Time)
//         {
//             // In the context of StrategyTester, this function gets called only when a new TesterExecutor run is launched
//             // and the bars need to be warmed up. This implies that the next operation will be getting "real-time" bars one-by-one
//             // and hence the currentPosition counter needs to be reset
//
//             if (to != _fromDt) throw new ArgumentException();
//
//             if (!_barsLoaded || from < _historicalBarsRequestedFrom)
//             {
//                 _historicalBarsRequestedFrom = from;
//                 // load bars for the optimization period plus the lookback period
//                 _bars = _historyProvider
//                     .GetBAUsByContract(contractId, from, _endDt)
//                     .ToList();
//                 _barsLoaded = true;
//
//                 _currentPosition = 0;
//                 while (_bars[_currentPosition].OpenDt < to) _currentPosition++;
//
//                 _historicalBarsCount = _currentPosition;
//             }
//
//             _currentPosition = _historicalBarsCount;
//             _currentInstant = _bars[_currentPosition].OpenDt;
//
//             return _bars.Take(_historicalBarsCount);
//         }
//
//         //public IEnumerable<ExchangeBar> GetBAUsByContract(long contractId, Instant to, int numberOfBars, Instant adjustTill, BarAggregationType aggType = BarAggregationType.Time, int futuresToUse = 0)
//         //{
//         //    if (to != _fromDt) throw new ArgumentException();
//
//         //    if (!_barsLoaded)
//         //    {
//         //        // load bars for the optimization period
//         //        _bars = _historyProvider
//         //            .GetBAUsByContract(contractId, _fromDt, _endDt)
//         //            .ToList();
//         //        _barsLoaded = true;
//         //    }
//
//         //    if (_historicalBarsRequested < numberOfBars)
//         //    {
//         //        _historicalBars = _historyProvider
//         //            .GetBAUsByContract(contractId, _fromDt, numberOfBars, _endDt)
//         //            .ToList();
//
//         //        // this is to avoid re-reading the storage every time
//         //        // in case it contains less bars than requested
//         //        _historicalBarsRequested = numberOfBars;
//         //    }
//
//
//         //    // every history request means that we need to start returning
//         //    // the bars from the first
//         //    _currentPosition = 0;
//         //    _currentInstant = _fromDt;
//
//         //    var adjNumberOfBars = numberOfBars / _timeframeMinutes;
//
//         //    if (_historicalBars.Count > adjNumberOfBars)
//         //    {
//         //        return _historicalBars
//         //            .Skip(_historicalBars.Count - adjNumberOfBars)
//         //            .Take(numberOfBars);
//         //    }
//
//         //    return _historicalBars;
//         //}
//
//         //private IEnumerable<ExchangeBar> LocateAndReturnHistory(Instant to)
//         //{
//         //    if (!_firstRealTimeRecordLocated)
//         //    {
//         //        var i = 0;
//         //        for (; i < _bars.Count; i++)
//         //        {
//         //            if (_bars[i].CloseDt <= to)
//         //            {
//         //                _historyBars.Add(_bars[i]);
//         //            }
//         //            else break;
//         //        }
//         //        _firstRealTimeRecord = i;
//         //        _firstRealTimeRecordLocated = true;
//         //    }
//         //    _currentInstant = _bars[_firstRealTimeRecord].OpenDt;
//         //    _currentPosition = _firstRealTimeRecord;
//
//         //    return _historyBars ?? new List<ExchangeBar>();
//         //}
//     }
// }
//
