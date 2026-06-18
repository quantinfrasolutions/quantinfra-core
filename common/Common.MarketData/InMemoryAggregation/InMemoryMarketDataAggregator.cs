using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Common.MarketData.Infrastructure;
using QuantInfra.Common.MarketData.OrderBooks;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.MarketData.InMemoryAggregation;

/// <summary>
/// Manages real-time market data (bars) and subscriptions to them.
/// Provides ability to claim a bar storage and handles updating it upon receiving new market data
/// </summary>
public sealed class InMemoryMarketDataAggregator : 
    IBarStorageProvider,
    IOrderBooksProvider,
    IBestBidOfferProvider
{
    private ILoggerFactory _loggerFactory;
    // private IProfiler? _profiler = Profiler.ActiveProfiler;
    
    private readonly Dictionary<string, HashSet<int>> _strategiesByBars = new();
    private readonly Dictionary<int, HashSet<int>> _strategiesByStream = new();
    private readonly Dictionary<int, HashSet<int>> _strategiesByBBO = new();
    private readonly Dictionary<int, HashSet<int>> _strategiesByOrderBook = new();
    
    private readonly Instant _initializationDt;
    private readonly HashSet<string> _syntheticBarStoragesQualifiers = new();
    private readonly Dictionary<int, List<AggregatingBarStorage>> _barsByStream = new();
    private readonly Dictionary<int, List<AggregatingBarStorage>> _syntheticBarStoragesByContractId = new();
    private readonly List<PeriodHistoryRequest> _periodHistoryRequests = new(); // Strategies that require BAUs
    
    private readonly Dictionary<int, double> _lastContractPrices = new();

    private Instant _currentInstant = Instant.MinValue;
    
    private readonly Dictionary<int, Dictionary<int, IBarStorage>> _directConversionPaths = new();
    private readonly Dictionary<int, Dictionary<int, IBarStorage>> _reverseConversionPaths = new();

    private readonly Dictionary<int, SubscriptionType> _streamSubscriptions = new();
    private readonly Dictionary<int, BestBidOffer?> _bbos = new();
    private readonly Dictionary<int, OrderBook?> _orderBooks = new();


    public InMemoryMarketDataAggregator(
        ILoggerFactory loggerFactory,
        Instant now
    )
    {
        _loggerFactory = loggerFactory;
        _initializationDt = now;
    }

    /// <summary>
    /// BarStorage Qualifier (StreamId.AggregationType.TimeFrame.Offset.Volume) => BarStorage
    /// </summary>
    public Dictionary<string, AggregatingBarStorage> Bars { get; } = new();
    
    public IBarStorage ClaimBarStorage(BarStorageConfig config, Func<int, Contract> getContract,
        int? strategyId = null)
    {
        if (!Bars.ContainsKey(config.FullQualifier))
        {
            int? streamId = null;
            
            var contract = getContract(config.Id);
            Contract? syntheticContract = null;
            
            if (config.IdType == IdType.Stream) streamId = config.Id;
            else
            {
                streamId = contract.DefaultStream?.StreamId ?? (contract.IsSynthetic()
                    ? -contract.ContractId
                    : throw new ArgumentException($"Contract {config.Id} is not synthetic and no default stream is configured for it")
                );
                if (contract.IsSynthetic()) syntheticContract = contract;
            }

            _streamSubscriptions.TryAdd(streamId.Value, SubscriptionType.Candles1M);


            var tradingSessions = config.TradingSessionIds?.Length > 0 
                ? contract.Template.TradingSessions.Where(ts => config.TradingSessionIds.Contains(ts.TradingSessionId)).ToList()
                : new List<TradingSession>();
            
            var bs = new AggregatingBarStorage( _loggerFactory, tradingSessions, config, streamId.Value);

            if (syntheticContract != null)
            {
                _syntheticBarStoragesQualifiers.Add(bs.FullQualifier);
                _syntheticBarStoragesByContractId.TryAdd(syntheticContract.ContractId, new List<AggregatingBarStorage>());
                _syntheticBarStoragesByContractId[syntheticContract.ContractId].Add(bs);

                var composition = syntheticContract.GetCurrentSyntheticContractComposition(_initializationDt)!;
                
                foreach (var und in composition.Weights)
                {
                    var childBs = ((IBarStorageProvider)this).ClaimBarStorage(
                        new BarStorageConfig(config)
                        {
                            Id = und.Key
                        },
                        getContract
                    );
                    bs.RequiredBarStorages.Add(childBs.FullQualifier, childBs);
                }
            }
            
            Bars.Add(config.FullQualifier, bs);
            _barsByStream.TryAdd(streamId.Value, new());
            _barsByStream[streamId.Value].Add(bs);
        }

        if (strategyId.HasValue)
        {
            _strategiesByBars.TryAdd(config.FullQualifier, new());
            _strategiesByBars[config.FullQualifier].Add(strategyId.Value);
        }
        
        return Bars[config.FullQualifier];
    }

    public IAggregatingBarStorage CreateBarStorage(BarStorageConfig config, Func<int, Contract> getContract,
        int? strategyId = null)
    {
        int? streamId = null;
        var contract = getContract(config.Id);
        Contract? syntheticContract = null;
        
        if (config.IdType == IdType.Stream) streamId = config.Id;
        else
        {
            streamId = contract.DefaultStream?.StreamId ?? (contract.IsSynthetic()
                    ? -contract.ContractId
                    : throw new ArgumentException($"Contract {config.Id} is not synthetic and no default stream is configured for it")
                );
            if (contract.IsSynthetic()) syntheticContract = contract;
        }
        
        _streamSubscriptions.TryAdd(streamId.Value, SubscriptionType.Candles1M);
        
        var tradingSessions = config.TradingSessionIds?.Length > 0 
            ? contract.Template.TradingSessions.Where(ts => config.TradingSessionIds.Contains(ts.TradingSessionId)).ToList()
            : new List<TradingSession>();
        
        return new AggregatingBarStorage(_loggerFactory, tradingSessions, config, streamId.Value);
    }

    public void ClaimExchangeBars(IdType idType, int id, Func<int, Contract> getContract, int strategyId,
        Period? lookback)
    {
        int? streamId = null;
        Contract? syntheticContract = null;
        
        if (idType == IdType.Stream) streamId = id;
        else
        {
            var contract = getContract(id);
            streamId = contract.DefaultStream?.StreamId ?? (contract.IsSynthetic()
                    ? -contract.ContractId
                    : throw new ArgumentException($"Contract {id} is not synthetic and no default stream is configured for it")
                );
            if (contract.IsSynthetic()) syntheticContract = contract;
        }

        _strategiesByStream.TryAdd(streamId!.Value, new());
        _strategiesByStream[streamId!.Value].Add(strategyId);

        var bs = ClaimBarStorage(new BarStorageConfig
        {
            IdType = idType,
            Id = id,
            Timeframe = Period.FromMinutes(1)
        }, getContract);
        bs.RegisterIndicator(new Close());

        if (lookback == null) return;
        
        var existingHR = _periodHistoryRequests
            .SingleOrDefault(r => r.IdType == idType && r.Id == id);

        if (existingHR == null)
        {
            _periodHistoryRequests.Add(new PeriodHistoryRequest(idType, id, lookback));
        }
        else
        {
            if ((existingHR.Period - lookback).ToDuration().TotalMilliseconds < 0)
            {
                _periodHistoryRequests.Remove(existingHR);
                _periodHistoryRequests.Add(new PeriodHistoryRequest(idType, id, lookback));
            }
        }
    }

    public double? GetLastPrice(int contractId) => 
        _lastContractPrices.TryGetValue(contractId, out var lastPrice) 
            ? lastPrice 
            : null;
    
    public void ClaimOrderBook(int contractId, int? strategyId = null)
    {
        _orderBooks.TryAdd(contractId, null);
        if (strategyId.HasValue)
        {
            _strategiesByOrderBook.TryAdd(contractId, new());
            _strategiesByOrderBook[contractId].Add(strategyId.Value);
        }
    }

    public void ClaimBestBidOffer(int contractId, int? strategyId = null)
    {
        _bbos.TryAdd(contractId, null);
        if (strategyId.HasValue)
        {
            _strategiesByBBO.TryAdd(contractId, new());
            _strategiesByBBO[contractId].Add(strategyId.Value);
        }
    }

    public IBarStorage this[string fullQualifier] => throw new System.NotImplementedException();
    public IOrderBook? this[int contractId] => _orderBooks[contractId];
    BestBidOffer? IBestBidOfferProvider.this[int contractId] => _bbos[contractId];
    

    /// <summary>
    /// Returns the list of bars that have been closed as the result of appending a new exchange bar
    /// </summary>
    /// <param name="bar"></param>
    /// <param name="contractId"></param>
    /// <returns></returns>
    public Dictionary<string, Instant> OnExchangeBar(ExchangeBar bar, int? contractId)
    {
        if (contractId.HasValue) _lastContractPrices[contractId.Value] = bar.Close;
        _currentInstant = bar.CloseDt;
        // Append bar to bar storages
        if (_barsByStream.ContainsKey(bar.StreamId))
        {
            #if PROFILE
            using (_profiler.Step("AppendBar"))
            {
            #endif

            var closedBars = _barsByStream[bar.StreamId]
                .OrderBy(b => b.BarStorageConfig.Timeframe.ToDuration())
                .Select(bs => new { FullQualifier = bs.FullQualifier, CloseDt = bs.AppendBar(bar) })
                .Where(x => x.CloseDt.HasValue)
                .ToDictionary(x => x.FullQualifier, x => x.CloseDt!.Value);
                  
            return closedBars;
            
            #if PROFILE
            }
            #endif
        }
        
        return new(0);
    }

    public void AddFxConversionContract(int contractId, Func<int, Contract> getContract)
    {
        var contract = getContract(contractId);
        
        var bs = ClaimBarStorage(new BarStorageConfig
        {
            AggregationType = BarAggregationType.Time,
            Id = contract.ContractId,
            IdType = IdType.Contract,
            LastValueOnly = true,
            Timeframe = Period.FromMinutes(1)
        }, getContract);
        bs.RegisterIndicator(new Close());

        var baseCurrencyId = contract.Template.BaseCurrency!.CurrencyId;
        var settlementCurrencyId = contract.Template.SettlementCurrency.CurrencyId;
        
        _directConversionPaths.TryAdd(baseCurrencyId, new());
        _directConversionPaths[baseCurrencyId].TryAdd(settlementCurrencyId, bs);
        _reverseConversionPaths.TryAdd(settlementCurrencyId, new());
        _reverseConversionPaths[settlementCurrencyId].TryAdd(baseCurrencyId, bs);
    }

    public Dictionary<string, Instant> OnExchangeTick(ExchangeTrade tick, Instant processingDt)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyCollection<int> GetStrategiesByBarQualifier(string barQualifier) =>
        _strategiesByBars.TryGetValue(barQualifier, out var strategies)
            ? strategies
            : Array.Empty<int>();

    public IReadOnlyCollection<int> GetStrategiesByExchangeBar(int streamId) =>
        _strategiesByStream.TryGetValue(streamId, out var strategies)
            ? strategies
            : Array.Empty<int>();
    
    public IReadOnlyCollection<int> GetStrategiesByBBO(int contractId) =>
        _strategiesByBBO.TryGetValue(contractId, out var strategies)
            ? strategies
            : Array.Empty<int>();
    
    public IReadOnlyCollection<int> GetStrategiesByOrderBook(int contractId) =>
        _strategiesByOrderBook.TryGetValue(contractId, out var strategies)
            ? strategies
            : Array.Empty<int>();

    public void OnOrderBookSnapshot(OrderBookSnapshot? snapshot, Instant processingDt)
    {
        if (snapshot == null) return;
        _orderBooks[snapshot.ContractId] = new OrderBook(snapshot);
    }

    public void OnOrderBookAggregatedUpdate(int contractId, IReadOnlyDictionary<decimal, decimal> updatedBids, 
        IReadOnlyDictionary<decimal, decimal> updatedAsks, Instant ts)
    {
        if (!_orderBooks.TryGetValue(contractId, out var ob) || ob == null) return;

        foreach (var bid in updatedBids)
        {
            ob.Update(BookSide.Bid, bid.Key, bid.Value, ts);
        }
        foreach (var ask in updatedAsks)
        {
            ob.Update(BookSide.Ask, ask.Key, ask.Value, ts);
        }
    }

    public void OnBestBidAskUpdated(int contractId, BestBidOffer bestBidAsk)
    {
        _bbos[contractId] = bestBidAsk;
    }

    public List<HistoryRequest> GetHistoryRequests() =>
        GetHistoryRequests(Bars.Values.Where(bs => !_syntheticBarStoragesQualifiers.Contains(bs.FullQualifier)), _initializationDt);
    
    public static List<HistoryRequest> GetHistoryRequests(IEnumerable<AggregatingBarStorage> bars, Instant initializationDt) =>
         bars.Where(bs => !bs.IsInitialized) 
            // Skip synthetic bar storages, because they are initialized by appending bars to the underlying storages
            // TODO: synthetics that are saved to MDS storage and can be retrieved directly
         .Select(bs =>
         {
             // Find all non-zero offsets of time zones within the desired interval
             var tz = DateTimeZoneProviders.Tzdb[bs.BarStorageConfig.Timezone];
             var interval = new Interval(initializationDt.Minus(Duration.FromMinutes(bs.CapacityInBAU)), initializationDt);
             var tzOffsetsInMinutes = tz.GetZoneIntervals(interval).Select(i => Math.Abs(i.WallOffset.Seconds / 60))
                 .Where(s => s != 0).Distinct().ToList();
             
             var tradingSessionsBoundariesOffsetsInMinutes = bs.TradingSessions?.Values
                 .SelectMany(ts => ts.Days)
                 .SelectMany(d => new List<int>
                 {
                     (int)((d.Start - LocalTime.Midnight).ToDuration().TotalSeconds / 60), 
                     (int)((d.End - LocalTime.Midnight).ToDuration().TotalSeconds / 60)
                 })
                 .Where(m => m != 0)
                 .Distinct().ToList()
                 ?? new();
             
             return new
             {
                 bs.Capacity,
                 bs.Timeframe,
                 bs.CapacityInBAU,
                 ReserveFactor = bs.GetBarsRequestReserveFactor(),
                 bs.BarStorageConfig.Id,
                 bs.BarStorageConfig.IdType,
                 bs.BarStorageConfig.Timezone,
                 tzOffsetsInMinutes,
                 tradingSessionsBoundariesOffsetsInMinutes,
                 TradingSessionIds = bs.BarStorageConfig.TradingSessionIds?.OrderBy(x => x).ToList() ?? new()
             };
         })
         .GroupBy(bs => new { bs.Id, bs.IdType, })            
         .Select(gr =>
         {
             // First, define the minimum timeframe
             var minResolutionMinutes = (int)gr.OrderBy(x => x.Timeframe.ToDuration().TotalMilliseconds)
                 .First().Timeframe.ToDuration().TotalMinutes;
             
             // If there are bars with different timezones, define the greatest divisor for their offsets
             string tz = "UTC";
             var distinctTimezones = gr.Select(i => i.Timezone).Distinct().ToList();
             if (distinctTimezones.Count > 1)
             {
                 var tzOffsetMinutes = GCD(gr.SelectMany(i => i.tzOffsetsInMinutes).Distinct());
                 minResolutionMinutes = Math.Min(minResolutionMinutes, tzOffsetMinutes);
             }
             else
             {
                 tz = distinctTimezones.First();
             }
             
             // If bars require different trading sessions, define the greatest divisor for their start and end times
             var tsOffsets = gr.SelectMany(i => i.tradingSessionsBoundariesOffsetsInMinutes).Distinct().ToList();
             if (tsOffsets.Count > 0)
             {
                 var tsOffsetMinutes = GCD(tsOffsets);
                 minResolutionMinutes = Math.Min(minResolutionMinutes, tsOffsetMinutes);
             }

             return new
             {
                 gr.Key.IdType,
                 gr.Key.Id,
                 RequiredBAUs = gr.OrderByDescending(i => i.CapacityInBAU * i.ReserveFactor).First(),
                 MinResolution = minResolutionMinutes,
                 tz
             };
         })
         .Select(gr => new HistoryRequest(
             gr.IdType,
             gr.Id,
             gr.RequiredBAUs.CapacityInBAU,
             gr.RequiredBAUs.ReserveFactor,
             Period.FromMinutes(gr.MinResolution),
             gr.tz
         ))
         .ToList();
    
    private static int GCD(int a, int b)
    {
        if (a < 0 || b < 0) throw new InvalidOperationException("GCD works only for positive numbers");
        while (a != 0 && b != 0)
        {
            if (a > b)
                a %= b;
            else
                b %= a;
        }

        return a | b;
    }
    
    private static int GCD(IEnumerable<int> values) => values.Aggregate(GCD);

    public IReadOnlyList<PeriodHistoryRequest> GetPeriodHistoryRequests() => _periodHistoryRequests;

    public void CopyFrom(InMemoryMarketDataAggregator aggregator, HashSet<long> synthContractsToReload)
    {
        foreach (var b in Bars)
        {
            if (aggregator.Bars.TryGetValue(b.Key, out var bs))
            {
                if (bs.Capacity >= b.Value.Capacity)
                {
                    // If there is a synth contract that has been rolled over and requires bars reloading,
                    // do not copy the existing bar storage
                    if (bs.BarStorageConfig.IdType == IdType.Contract 
                        && synthContractsToReload.Contains(bs.BarStorageConfig.Id)
                    ) continue;
                    
                    bs.CopyFrom(b.Value);
                }
            }
        }
    }
    
    public double? GetConversionRate(int fromCcyId, int toCcyId, Instant dt)
    {
        if (_currentInstant > dt) return null;

        if (_directConversionPaths.TryGetValue(fromCcyId, out var dtos)
            && dtos.TryGetValue(toCcyId, out var dbs) && dbs.CurrentBar != null)
            return dbs.CurrentBar.Close;

        if (_reverseConversionPaths.TryGetValue(fromCcyId, out var rtos)
            && rtos.TryGetValue(toCcyId, out var rbs) && rbs.CurrentBar != null)
            return 1 / rbs.CurrentBar.Close;

        return null;
    }

    public void Clear()
    {
        foreach (var b in Bars.Values)
        {
            b.Clear();
        }
    }

    public IReadOnlyCollection<IMarketDataSubscription> GetMarketDataSubscriptions() => _streamSubscriptions
        .Select(kv => new MarketDataSubscription(kv.Key, kv.Value))
        .ToList();

    public IReadOnlyCollection<int> GetBestBidOfferContractIds() => _bbos.Keys;
    public IReadOnlyCollection<int> GetOrderBookContractIds() => _orderBooks.Keys;
}

    class MarketDataSubscription : IMarketDataSubscription
    {
        public MarketDataSubscription(int streamId, SubscriptionType subscriptionType)
        {
            StreamId = streamId;
            SubscriptionType = subscriptionType;
        }

        public int SubscriptionId { get; } = 0;
        public int? StreamId { get; }
        public SubscriptionType SubscriptionType { get; }
    }