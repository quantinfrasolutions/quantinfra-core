using System;
using System.Collections.Generic;
using System.Linq;
using Common.Backtesting;
using Common.MarketData;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;

namespace BacktestingCore.Providers;

/// <summary>
/// Provides an in-memory cache of bars for backtesting
/// </summary>
/// <remarks>
/// * At the first history request for each contract (that is performed to initialize bar storages) requests data
///     from the storage between the GetBAUs' "from" date and the test end date. The "historical" data (before the
///     backtest's Start date) is saved to the cache. The "real-time" data is saved to another cache.
/// * Returns the retrieved history and sets the clock to the first non-historical bar
/// * If the history is requested again, will try to return existing data from the history cache. If the requested "from"
///     date is earlier than the one existing in the cache, missing data will be requested from the storage and the history cache
///     will be updated
/// * During the first "real-time" run will sequentially read from the yet unordered real-time caches and populate a new cache with the sorted data.
///     The bar with the least CloseDt will be chosen as the next one.
/// * During the subsequent real-time runs will read already ordered bars directly from the sorted cache
/// </remarks>
public class InMemoryCandlesStorage : ITestMarketDataProvider
{
    private readonly ILogger<InMemoryCandlesStorage> _logger;
    private readonly IMarketDataHistoryProvider _historyProvider;
    private readonly Instant _fromDt;
    private readonly Instant _endDt;
    private readonly bool _useCache;

    private readonly HashSet<int> _realTimeLoadedForContracts = new();
    private readonly List<RealTimeCache> _realTimeBars = new();
    private readonly Dictionary<int, Dictionary<Instant, HistoryCache>> _historicalBaus = new();
    private readonly Dictionary<int, Dictionary<Instant, HistoryCache>> _historicalBars = new();
    private readonly Dictionary<int, double> _lastKnownPrices = new();
    private readonly Dictionary<Instant, Dictionary<int, double>> _lastKnownPricesCache = new();
    
    Instant _currentInstant;
    private bool _multipleContracts;
    
    private readonly List<int> _currentPositions = new();
        
    private List<ExchangeBar> _orderedCache = new();
    private int _sequencePosition = -1;

    public InMemoryCandlesStorage(
        InMemoryCandlesStorageOptions options,
        IMarketDataHistoryProvider historyProvider,
        ILogger<InMemoryCandlesStorage> logger
    )
    {
        _historyProvider = historyProvider;
        _logger = logger;
        _fromDt = options.StartDt;
        _endDt = options.EndDt;
        _useCache = options.UseCache;
        _currentInstant = options.StartDt;
    }
        
    public ExchangeBar GetNextBar()
    {
        // this is the second run, so the sequencer is initialized
        if (_sequencePosition != -1)
        {
            if (_sequencePosition < _orderedCache.Count)
            {
                var seqBar = _orderedCache[_sequencePosition];
                _currentInstant = seqBar.CloseDt;
                _sequencePosition++;
                return seqBar;
            }
            
            return null; // All bars processed
        }

        RealTimeCache chosenCache;
        if (_multipleContracts)
        {
            var curInstant = Instant.MaxValue;
            var index = 0;
            var barsExist = false;
            for (var i = 0; i < _realTimeBars.Count; i++)
            {
                var cache = _realTimeBars[i];
                if (cache.Sequence == cache.Bars.Count)
                {
                    continue;
                }

                barsExist = true;
                var closeDt = cache.Bars[cache.Sequence].CloseDt;
                if (closeDt < curInstant)
                {
                    curInstant = closeDt;
                    index = i;
                }
            }

            if (!barsExist) return null;

            chosenCache = _realTimeBars[index];
        }
        else
        {
            chosenCache = _realTimeBars[0];
        }

        if (chosenCache.Sequence == chosenCache.Bars.Count) return null;
        
        var res = chosenCache.Bars[chosenCache.Sequence];
        if (chosenCache.ContractId.HasValue && chosenCache.Sequence != 0)
            _lastKnownPrices[chosenCache.ContractId.Value] = chosenCache.Bars[chosenCache.Sequence - 1].Close;
        chosenCache.Sequence++;
        if (_useCache) _orderedCache.Add(res);
        _currentInstant = res.CloseDt;
        return res;
    }

    public void Restart()
    {
        _currentInstant = _fromDt;
        _lastKnownPrices.Clear();
        if (_useCache)
        {
            _sequencePosition = 0;
                
            // If the ordered cache is already initialized, we will read from it and do not need the individual caches,
            // so we can free the memory
            _realTimeBars.Clear();
            _currentPositions.Clear();
        }
        else
        {
            for (var i = 0; i < _currentPositions.Count; i++) _currentPositions[i] = 0;            
        }
    }

    public Instant GetCurrentInstant() => _currentInstant;

    public IReadOnlyDictionary<int, double> GetLastKnownPrices(IEnumerable<int> contractIds, Instant dt)
    {
        if (!_useCache)
        {
            return _lastKnownPrices;
        }

        if (!_lastKnownPricesCache.ContainsKey(dt))
        {
            _lastKnownPricesCache.Add(dt, _lastKnownPrices.ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        return _lastKnownPricesCache[dt];
    }
        

    public IEnumerable<ExchangeBar> GetBAUsByStream(int streamId, Instant from, Instant to)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to)
    {
        HistoryCache cache;
        if (_historicalBaus.TryGetValue(contractId, out var caches)
            && caches.TryGetValue(to, out cache))
        {
            // If there is not enough history, request the missing part and persist it in the cache
            if (cache.HistoryStartDt > from)
            {
                _logger.LogDebug($"Requesting missing bars for contract {contractId} from {from} to {cache.HistoryStartDt}");
                var missingBars = _historyProvider.GetBAUsByContract(contractId, from, cache.HistoryStartDt).ToList();
                cache.HistoryStartDt = from;
                cache.Bars = missingBars.Concat(cache.Bars).OrderBy(b => b.CloseDt).ToList();
                _logger.LogDebug($"Received {missingBars.Count} bars for contract {contractId}, total historical bars count is {cache.Bars.Count}");
            }
        }
        else
        {
            // History doesn't exist for the requested "to"
            _historicalBaus.TryAdd(contractId, new());

            cache = new HistoryCache
            {
                HistoryStartDt = from
            };
            _historicalBaus[contractId].Add(to, cache);

            if (!_realTimeLoadedForContracts.Contains(contractId))
            {
                _logger.LogDebug($"Requesting historical and real-time bars for contract {contractId} from {from} to {_endDt}");
                var allBars = _historyProvider.GetBAUsByContract(contractId, from, _endDt).ToList();
                var realtimeIndex = allBars.FindIndex(b => b.CloseDt > to);
                cache.Bars = allBars.Take(realtimeIndex).ToList();
                _realTimeBars.Add(new RealTimeCache { Bars = allBars.Skip(realtimeIndex).ToList(), ContractId = contractId });
                _realTimeLoadedForContracts.Add(contractId);
                _multipleContracts = _realTimeLoadedForContracts.Count > 1;
                _logger.LogDebug($"Received {cache.Bars.Count} for contract {contractId}");
                _currentPositions.Add(0);

                if (_useCache)
                {
                    // Expand the cache if required, preserving the current data
                    _orderedCache.Capacity = Math.Max(_realTimeBars.Sum(i => i.Bars.Count), _orderedCache.Capacity);
                }
            }
            else
            {
                _logger.LogDebug($"Requesting historical bars for contract {contractId} from {from} to {_endDt}");
                var historicalBars = _historyProvider.GetBAUsByContract(contractId, from, to).ToList();
                cache.Bars = historicalBars;
                _logger.LogDebug($"Received {cache.Bars.Count} for contract {contractId}");
            }
        }
        
        return cache.Bars;
    }

    public IEnumerable<ExchangeBar> GetAggregatedCandlesByContract(int contractId, Instant from, Instant to,
        Period timeframe, string timezone)
    {
        // TODO: ensure the cache timeframe is not lower than the requested timeframe
        HistoryCache cache;
        if (_historicalBars.TryGetValue(contractId, out var caches)
            && caches.TryGetValue(to, out cache))
        {
            // If there is not enough history, request the missing part and persist it in the cache
            if (cache.HistoryStartDt > from)
            {
                _logger.LogDebug($"Requesting missing bars for contract {contractId} from {from} to {cache.HistoryStartDt}");
                var missingBars = _historyProvider.GetAggregatedCandlesByContract(contractId, from, cache.HistoryStartDt, timeframe, timezone).ToList();
                // TODO: assure that the last received bar doesn't overlap with the first bar in the existing history
                cache.HistoryStartDt = from;
                cache.Bars = missingBars.Concat(cache.Bars).OrderBy(b => b.CloseDt).ToList();
                _logger.LogDebug($"Received {missingBars.Count} bars for contract {contractId}, total historical bars count is {cache.Bars.Count}");
            }
        }
        else
        {
            // History doesn't exist for the requested "to"
            _historicalBars.TryAdd(contractId, new());

            cache = new HistoryCache
            {
                HistoryStartDt = from
            };
            _historicalBars[contractId].Add(to, cache);

            _logger.LogDebug($"Requesting historical bars for contract {contractId} from {from} to {_endDt}");
            var historicalBars = _historyProvider.GetAggregatedCandlesByContract(contractId, from, to, timeframe, timezone).ToList();
            cache.Bars = historicalBars;
            _logger.LogDebug($"Received {cache.Bars.Count} for contract {contractId}");
        }
        
        return cache.Bars;
    }

    public IEnumerable<ExchangeBar> GetAggregatedBausByStream(int streamId, Instant from, Instant to, Period timeframe,
        string timezone)
    {
        throw new NotImplementedException();
    }
}

class HistoryCache
{
    public Instant HistoryStartDt { get; set; }
    public IReadOnlyList<ExchangeBar> Bars { get; set; }
}

class RealTimeCache
{
    public int Sequence { get; set; }
    public int? ContractId { get; init; }
    public IReadOnlyList<ExchangeBar> Bars { get; init; }
}