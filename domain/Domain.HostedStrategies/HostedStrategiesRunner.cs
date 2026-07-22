using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Common.Metrics;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;
using Prometheus;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.MarketData;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Common.MarketData.InMemoryAggregation;
using QuantInfra.Common.Strategies;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Domain.Events.Accounts.AccountsService;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.HostedStrategies;

public sealed class HostedStrategiesRunner :
    IEventHandler<BalanceOperationProcessedEvt>,
    IEventHandler<ExecutionReportEvt>,
    IEventHandler<TradeEvt>,
    IEventHandler<AccountsServiceHeartbeatEvt>,

    IEventHandler<Candle1MClosedEvt>,
    IEventHandler<AggregatedOrderbookUpdateEvt>,
    IEventHandler<BestBidAskUpdatedEvt>,

    IAsyncQueryResponseHandler<GetOrderBookSnapshot, OrderBookSnapshot?>,
    
    IHeartbeatsProvider
{
    private readonly HostedStrategiesRunnerConfig _config;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHostedStrategiesFactory _hostedHostedStrategiesFactory;
    private readonly ILogger _logger;
    private readonly IClock _clock;
    private readonly IQueryBus _queryBus;
    private readonly bool _writePerformanceMetrics;
    private readonly Stopwatch _sw = new();
    
    private Dictionary<int, int> _strategiesByAccount;
    private Dictionary<int, AbstractHostedStrategy> _strategies;
    private Dictionary<int, AccountRecordV6> _accountByStrategy;
    // private Dictionary<int, AbstractHostedExecutionStrategy> _esas;
    private readonly HashSet<int> _heartbeatStrategies = new();
    
    private readonly Histogram? _totalMetric;
    private readonly Histogram? _serviceMetric;
    private readonly Histogram? _aggMetric;
    private readonly Histogram? _strategiesProcessClosedBarMetric;

    public HostedStrategiesRunner(
        HostedStrategiesRunnerConfig config, 
        ILoggerFactory loggerFactory,
        IHostedStrategiesFactory hostedHostedStrategiesFactory,
        ILogger<HostedStrategiesRunner> logger,
        IClock clock, 
        IQueryBus queryBus
    )
    {
        _config = config;
        _loggerFactory = loggerFactory;
        _hostedHostedStrategiesFactory = hostedHostedStrategiesFactory;
        _logger = logger;
        _clock = clock;
        _queryBus = queryBus;

        Aggregator = new(_loggerFactory, clock.GetCurrentInstant());

        if (config.WritePerformanceMetrics)
        {
            _writePerformanceMetrics = true;
            _totalMetric = MetricsDefinition.OnExchangeBarProcessingTime;
            _serviceMetric = MetricsDefinition.OnExchangeBarServiceTime;
            _aggMetric = MetricsDefinition.OnExchangeBarAggTime;
            _strategiesProcessClosedBarMetric = MetricsDefinition.OnExchangeBarStrategiesTime;
        }
    }
    
    public InMemoryMarketDataAggregator Aggregator { get; }

    public void Initialize(
        IReadOnlyCollection<Strategy> strategies,
        // IReadOnlyCollection<EsaSubscription> esaSubscriptions,
        IReadOnlyCollection<AccountRecordV6> accounts
    )
    {
        _strategiesByAccount = strategies.ToDictionary(s => s.AccountId, s => s.StrategyId);
        _accountByStrategy = strategies.ToDictionary(s => s.StrategyId, s => accounts.Single(a => a.AccountId == s.AccountId));
        
        _strategies = strategies
            .Where(s => s.Status.IsActive())
            .Select(_hostedHostedStrategiesFactory.CreateHostedStrategy)
            .ToDictionary(s => s.StrategyId);

        // _esas = esaSubscriptions
        //     .Where(s => s.SubscriptionStatus.IsActive())
        //     .Select(s => new { s.ExecutableSubaccountId, Strategy = _hostedHostedStrategiesFactory.CreateHostedExecutionStrategy(/*TODO*/) })
        //     .ToDictionary(s => s.ExecutableSubaccountId, s => s.Strategy);
        
        
        foreach (var strategy in _strategies.Values)
        {
            strategy.Initialize(new InitContext(_accountByStrategy[strategy.StrategyId], _queryBus), Aggregator, Aggregator, Aggregator, this, _loggerFactory);
        }
    }
    
    // TODO: make an async implementation
    public void LoadMarketData(Instant now, IMarketDataHistoryProvider mdHistoryProvider, bool forceLoadBaus = false)
    {
        _logger.LogInformation("Loading market data");

        var barRequests = Aggregator.GetHistoryRequests();
        var skipHistoryOffset = _config.SkipProcessingOfHistoryBeforeFromNow;
        var nowUtc = now.InUtc();
        now = Instant.FromUtc(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, nowUtc.Minute,
            0); // Round to the previous minute
        var skipHistoryTill = now.Minus(skipHistoryOffset);
        var periodBarRequests = Aggregator.GetPeriodHistoryRequests();

        var barRequestsResultsBuffer = barRequests.Select(br => new BarRequest
            {
                Id = br.Id,
                IdType = br.IdType,
                BausFrom = skipHistoryTill,
                NumberOfBaus = (int)(br.NumberOfBaus * br.ReserveFactor),
                MinResolution = br.MinResolution,
                Timezone = br.Timezone,
            }
        ).ToList();

        List<List<ExchangeBar>> bars;

        // request bars until the max number of attempts is reached
        var completedAttempts = 0;
        var initialized = false;

        foreach (var s in _strategies.Values)
        {
            s.Context = CreateStrategyContext(s.StrategyId, true, Instant.MinValue, now);
        }

        do
        {
            Aggregator.Clear();
            
            bars = barRequestsResultsBuffer.Select(r =>
            {
                // This function may be called several times, if there is not enough bars in the bar storage after the last loading.
                // The logic is the following:
                // * We need to load BAUs since skipHistoryTill or the initial date of the period's request, whichever is earlier
                // * For earlier bars, we load the aggregated bars with the minimal resolution used by bar storages with the current symbol

                if (r.IsEnoughBars) return r.ReceivedBars;

                // if (r.Id == 20214) Debugger.Break();

                if (!r.From.HasValue) // First call of the function
                {
                    ZonedDateTime fromUnadjusted;

                    // By default, request the BAUs since the moment of real-time processing
                    r.BausFrom = skipHistoryTill;
                    // and request aggregated bars since the earliest requested moment
                    fromUnadjusted = now.Minus(Duration.FromMinutes(r.NumberOfBaus)).InUtc();

                    var periodRequest = periodBarRequests.SingleOrDefault(pr => pr.IdType == r.IdType && pr.Id == r.Id);

                    if (periodRequest != null)
                    {
                        var periodRequestStart = now.Minus(periodRequest.Period.ToDuration());

                        if (periodRequestStart < r.BausFrom) r.BausFrom = periodRequestStart;
                        if (periodRequestStart < fromUnadjusted.ToInstant())
                            fromUnadjusted = periodRequestStart.InUtc();
                    }

                    // round the From timestamp to the beginning of the month
                    r.From = new LocalDate(fromUnadjusted.Year, fromUnadjusted.Month, 1);
                }
                else
                {
                    // If there was not enough bars loaded during the previous iteration, move the start period earlier by 1 month
                    r.From = r.From.Value.Minus(Period.FromMonths(1));
                }

                var from = Instant.FromUtc(r.From.Value.Year, r.From.Value.Month, r.From.Value.Day, 0, 0);
                if (from < r.BausFrom)
                {
                    _logger.LogInformation(
                        $"Requesting aggregated bars for {r.IdType}({r.Id}): from={from}, to={r.BausFrom}, tf={PeriodPattern.Roundtrip.Format(r.MinResolution)}");

                    var res = r.IdType == IdType.Contract
                        ? mdHistoryProvider.GetAggregatedCandlesByContract(r.Id, from, r.BausFrom, r.MinResolution,
                            r.Timezone)
                        : mdHistoryProvider.GetAggregatedBausByStream(r.Id, from, r.BausFrom, r.MinResolution,
                            r.Timezone); // TODO: async methods
                    r.ReceivedBars = res.ToList();

                    if (r.ReceivedBars.Count > 0)
                    {
                        var lastBar = r.ReceivedBars.Last();
                        if (lastBar.CloseDt > r.BausFrom)
                        {
                            // If the close of the last bar is later than the initial moment of the baus, remove it and load the baus since the close of the previous bar
                            // (i.e., the open of the removed)
                            r.BausFrom = lastBar.OpenDt;
                            r.ReceivedBars.RemoveAt(r.ReceivedBars.Count - 1);
                        }
                    }
                }

                if (r.BausFrom != now || forceLoadBaus)
                {
                    _logger.LogInformation($"Requesting BAUs for {r.IdType}({r.Id}): from={r.BausFrom}");

                    var res = r.IdType == IdType.Contract
                        ? mdHistoryProvider.GetBAUsByContract(r.Id, r.BausFrom, now)
                        : mdHistoryProvider.GetBAUsByStream(r.Id, r.BausFrom, now); // TODO: async methods

                    r.ReceivedBars.AddRange(res);
                }


                r.IsEnoughBars = true;

                return r.ReceivedBars;
            }).ToList();
            
            var sorted = bars.SelectMany(b => b).OrderBy(b => b.CloseDt).ToList();
            foreach (var b in sorted)
            {
                OnExchangeBar(b, true, now, false);
            }

            initialized = true;
            foreach (var bs in Aggregator.Bars.Values.Where(bs => !bs.IsInitialized))
            {
                _logger.LogDebug(
                    $"Not enough capacity for bs {bs.FullQualifier}: capacity={bs.Capacity}, count={bs.Count}");
                var result = barRequestsResultsBuffer.SingleOrDefault(r =>
                    r.IdType == ((BarStorage)bs).BarStorageConfig.IdType &&
                    r.Id == ((BarStorage)bs).BarStorageConfig.Id);
                if (result != null)
                {
                    result.IsEnoughBars = false;
                }
                else
                {
                    // A bar storage for a synthetic contract
                    var dependantBs = bs.RequiredBarStorages.Values;
                    var dependantRequests = barRequestsResultsBuffer.Where(r =>
                        dependantBs.Any(dbs =>
                            dbs.BarStorageConfig.IdType == r.IdType && dbs.BarStorageConfig.Id == r.Id)).ToArray();
                    foreach (var dbr in dependantRequests)
                    {
                        dbr.IsEnoughBars = false;
                    }
                }

                initialized = false;
            }

            completedAttempts++;
            if (initialized) break;

        } while (completedAttempts < _config.RequestBarAttempts);

        if (!initialized)
        {
            _logger.LogWarning($"Some bar storages were not initialized after {completedAttempts} attempts");
        }
        else
        {
            _logger.LogInformation($"All bars initialized after {completedAttempts} attempts");
        }


        // OnBeforeProcessBarsHistory?.Invoke(service);
        // _calculationService = service;
        // service.Runner.ProcessBarsHistory(bars.SelectMany(b => b).OrderBy(b => b.CloseDt).ToList(), 
        // _clock.GetCurrentInstant(), sdProvider, queryBus, eventBus, false);

        foreach (var bs in Aggregator.Bars.Values)
        {
            bs.SetInitialized();
        }
        
        foreach (var s in _strategies.Values) s.ClearContext();
    }

    public void Deploy(Instant ts)
    {
        foreach (var strategy in _strategies.Values)
        {
            var context = CreateStrategyContext(strategy.StrategyId, false, ts, ts);
            if (!context.IsOk)
            {
                _logger.LogWarning($"Strategy {strategy.StrategyId} failed to create context for deploy: {context.GetMissingInfoLogString()}");
                continue;
            }

            if (context.StrategyRecord!.Status.IsActive())
            {
                strategy.Context = context;
                strategy.Deploy();
                strategy.ClearContext();
            }
        }

        // foreach (var kv in _esas)
        // {
        //     var context = CreateExecutionContext(kv.Key, ts);
        //     if (!context.IsOk)
        //     {
        //         _logger.LogWarning($"ESA {kv.Key} failed to create context for deploy: {context.GetMissingInfoLogString()}");
        //         Debugger.Break();
        //         continue;
        //     }
        //
        //     if (context.Subscription!.SubscriptionStatus.IsActive())
        //     {
        //         var esa = kv.Value;
        //         esa.Context = context;
        //         kv.Value.Deploy();
        //         esa.ClearContext();
        //     }
        // }
    }

    public void OnExchangeBar(ExchangeBar bar, bool isHistory, Instant processingDt) =>
        OnExchangeBar(bar, isHistory, processingDt, true);
    
    private void OnExchangeBar(ExchangeBar bar, bool isHistory, Instant processingDt, bool createContext)
    {
        long beginTs = 0;
        if (_writePerformanceMetrics && !isHistory) beginTs = MetricsUtils.GetUnixMicro();
        var service = 0L;
        var strategies = 0L;
        
        foreach (var s in Aggregator.GetStrategiesByExchangeBar(bar.StreamId))
        {
            long co = 0; 
            if (_writePerformanceMetrics && !isHistory) co = MetricsUtils.GetUnixMicro();
            var strategy = _strategies[s];
            HostedStrategyCalculationContext context;
            
            if (createContext || strategy.Context == null)
            {
                context = CreateStrategyContext(s, isHistory, bar.CloseDt, processingDt);

                if (!context.IsOk)
                {
                    _logger.LogWarning(
                        $"Strategy {s} failed to create context for bar {bar.CloseDt}: {context.GetMissingInfoLogString()}");
                    continue;
                }
                strategy.Context = context;
            }
            else
            {
                context = strategy.Context;
                context.ReferenceDt = bar.CloseDt;
            }
            

            if (!isHistory && bar.CloseDt <= context.StrategyState!.LastCalculationTs) continue; // dedup
            if (!context.StrategyRecord!.Status.IsActive()) continue;
            
            long co2 = 0;
            if (_writePerformanceMetrics && !isHistory) co2 = MetricsUtils.GetUnixMicro();
            
            strategy.ProcessExchangeBar(bar);
            strategy.ClearContext();
            
            long co3 = 0;
            if (_writePerformanceMetrics && !isHistory) co3 = MetricsUtils.GetUnixMicro();
            service += co2 - co;
            strategies += co3 - co2;
        }
        long barBegin = 0;
        if (_writePerformanceMetrics && !isHistory) barBegin = MetricsUtils.GetUnixMicro();
        var closedBars = Aggregator.OnExchangeBar(bar, bar.ContractId);
        long bars = 0;
        if (_writePerformanceMetrics && !isHistory) bars = MetricsUtils.GetUnixMicro() - barBegin;
        
        foreach (var bsQualifier in closedBars.Keys)
        {
            foreach (var s in Aggregator.GetStrategiesByBarQualifier(bsQualifier))
            {
                long co1 = 0;
                if (_writePerformanceMetrics && !isHistory) co1 = MetricsUtils.GetUnixMicro();
                
                var strategy  = _strategies[s];
                
                HostedStrategyCalculationContext context;
                if (createContext || strategy.Context == null)
                {
                    context = CreateStrategyContext(s, isHistory, bar.CloseDt, processingDt);

                    if (!context.IsOk)
                    {
                        _logger.LogWarning(
                            $"Strategy {s} failed to create context for bar {bar.CloseDt}: {context.GetMissingInfoLogString()}");
                        continue;
                    }
                    strategy.Context = context;
                }
                else
                {
                    context = strategy.Context;
                    context.ReferenceDt = bar.CloseDt;
                }
                
                if (!context.StrategyRecord.Status.IsActive()) continue;
                
                long co2 = 0;
                if (_writePerformanceMetrics && !isHistory) co2 = MetricsUtils.GetUnixMicro();
                
                strategy.Context = context;
                strategy.ProcessClosedBar(bsQualifier);
                strategy.ClearContext();
                long co3 = 0;
                if (_writePerformanceMetrics && !isHistory)
                {
                    co3 = MetricsUtils.GetUnixMicro();
                    _strategiesProcessClosedBarMetric!.Observe(co3 - co2);
                }

                service += co3 - co1;
            }
        }

        if (_writePerformanceMetrics && !isHistory)
        {
            _totalMetric!.Observe(MetricsUtils.GetUnixMicro() - beginTs);
            _serviceMetric?.Observe(service);
            _aggMetric?.Observe(bars);
        }
    }
    
    public void OnExchangeTick(ExchangeTrade tick)
    {
        throw new NotImplementedException();
    }

    public void OnOrderBookSnapshot(OrderBookSnapshot? snapshot, Instant processingDt)
    {
        Aggregator.OnOrderBookSnapshot(snapshot, processingDt);
    }
    
    public void OnAggregatedOrderBookUpdateEvt(AggregatedOrderbookUpdateEvt evt, Instant processingDt)
    {
        Aggregator.OnOrderBookAggregatedUpdate(evt.ContractId, evt.UpdatedBids, evt.UpdatedAsks, evt.Timestamp);
        foreach (var sid in Aggregator.GetStrategiesByOrderBook(evt.ContractId))
        {
            var context = CreateStrategyContext(sid, false, evt.Timestamp, processingDt);
            if (!context.StrategyRecord!.Status.IsActive()) continue;
            
            if (!context.IsOk)
            {
                _logger.LogWarning(
                    "Strategy {sid} failed to create context for order book update, contractId={cid}, ts={ts}, error={error}",
                    sid, evt.ContractId, evt.Timestamp, context.GetMissingInfoLogString()
                );
                continue;
            }
            var strategy  = _strategies[sid];
            strategy.Context = context;
            strategy.ProcessOrderbookL2Update(evt.ContractId, evt.UpdatedBids, evt.UpdatedAsks);
        }
    }

    public void OnBestBidAskUpdated(BestBidAskUpdatedEvt evt, Instant processingDt)
    {
        Aggregator.OnBestBidAskUpdated(evt.ContractId, new(evt.Bid, evt.Ask, evt.Timestamp));
        foreach (var sid in Aggregator.GetStrategiesByBBO(evt.ContractId))
        {
            var context = CreateStrategyContext(sid, false, evt.Timestamp, processingDt);
            if (!context.StrategyRecord!.Status.IsActive()) continue;
            
            if (!context.IsOk)
            {
                _logger.LogWarning(
                    "Strategy {sid} failed to create context for BBO update, contractId={cid}, ts={ts}, error={error}",
                    sid, evt.ContractId, evt.Timestamp, context.GetMissingInfoLogString()
                );
                continue;
            }
            var strategy  = _strategies[sid];
            strategy.Context = context;
            strategy.ProcessBestBidOfferUpdate(evt.ContractId, evt.Bid, evt.Ask);
        }
    }

    public void Handle(ExecutionReportEvt evt)
    {
        var er = evt.ExecutionReport;
        
        if (_strategiesByAccount.TryGetValue(er.AccountId, out var strategyId))
        {
            var strategy = _strategies[strategyId];
            
            HostedStrategyCalculationContext context;
            bool contextCreated = false;
            // When backtesting, ExecutionReports are received immediately (inside OnExchangeBar), so the context already exists
            if (strategy.Context == null)
            {
                context = CreateStrategyContext(strategyId, false, evt.Timestamp, _clock.GetCurrentInstant());
                strategy.Context = context;
                contextCreated = true;
            }
            else
            {
                context = strategy.Context;
            }
            
            if (!context.IsOk)
            {
                _logger.LogWarning($"Strategy {strategyId} failed to create context for ER {er.ExecId}");
                {
                    if (contextCreated) strategy.ClearContext();
                    return;
                }
            }

            if (!context.StrategyRecord!.Status.IsActive())
            {
                if (contextCreated) strategy.ClearContext();
                return;
            }
            _logger.LogDebug("Strategy {strategyId}: processing ER {er}", strategyId, er.GetLogString());
            
            strategy.Context = context;
            strategy.ProcessExecutionReport(er);
            if (contextCreated) strategy.ClearContext();
        }
        // else if (_esas.TryGetValue(er.AccountId, out var esa))
        // {
        //     var context = CreateExecutionContext(er.AccountId, _clock.GetCurrentInstant());
        //     if (!context.IsOk)
        //     {
        //         _logger.LogWarning($"ESA {er.AccountId} failed to create context for ER {er.ExecId}");
        //         return;
        //     }
        //     if (!context.Subscription!.SubscriptionStatus.IsActive()) return;
        //     _logger.LogDebug($"ESA {er.AccountId}: processing ER {er.GetLogString()}");
        //     esa.Context = context;
        //     esa.ProcessExecutionReport(er);
        //     esa.ClearContext();
        // }
    }
    
    public void Handle(BalanceOperationProcessedEvt evt)
    {
        // var bo = e.BalanceOperation;
        //
        // if (!_strategiesByAccount.TryGetValue(bo.AccountId, out var strategyId))
        // {
        //     _logger.LogWarning($"Received unneeded BO for account {bo.AccountId}, ignoring");
        //     return;
        // }
        //
        // if (!bo.AffectsInvestment) return;
        //
        // var context = CreateContext(strategyId, false, e.Timestamp, _clock.GetCurrentInstant());
        // if (!context.StrategyRecord.Status.IsActive()) return;
        //
        // _logger.LogInformation($"Strategy {strategyId}: processing BO {bo}");
        // _strategies[strategyId].ProcessInvestmentChange();
    }

    public void Handle(TradeEvt evt)
    {
        var trade = evt.Trade;
        
        if (_strategiesByAccount.TryGetValue(trade.AccountId, out var strategyId))
        {
            var context = CreateStrategyContext(strategyId, false, evt.Timestamp, _clock.GetCurrentInstant());
            if (!context.IsOk)
            {
                _logger.LogWarning($"Strategy {strategyId} failed to create context for trade {trade.TradeId}: {context.GetMissingInfoLogString()}");
                Debugger.Break();
                return;
            }
            if (!context.StrategyRecord.Status.IsActive()) return;
        
            _logger.LogDebug($"Strategy {strategyId}: processing trade {trade}");
            var strategy = _strategies[strategyId];
            strategy.Context = context;
            strategy.ProcessTrade(trade);
            strategy.ClearContext();
        }
        // if (_esas.TryGetValue(trade.AccountId, out var esa))
        // {
        //     var context = CreateExecutionContext(trade.AccountId, _clock.GetCurrentInstant());
        //     if (!context.IsOk)
        //     {
        //         _logger.LogWarning("ESA {accountId} failed to create context for trade {tradeId}", trade.AccountId, trade.TradeId);
        //         return;
        //     }
        //     if (!context.Subscription!.SubscriptionStatus.IsActive()) return;
        //
        //     _logger.LogDebug("Processing trade {trade}", trade);
        //     esa.Context = context;
        //     esa.ProcessTrade(trade);
        //     esa.ClearContext();
        // }
    }
    
    public void Handle(Candle1MClosedEvt evt) => OnExchangeBar(evt.Bar, false, _clock.GetCurrentInstant());
    public void Handle(AggregatedOrderbookUpdateEvt evt) => OnAggregatedOrderBookUpdateEvt(evt, _clock.GetCurrentInstant());
    public void Handle(AsyncQueryResponse<GetOrderBookSnapshot, OrderBookSnapshot?> response) => OnOrderBookSnapshot(response.Result, _clock.GetCurrentInstant());
    public void Handle(BestBidAskUpdatedEvt evt) => OnBestBidAskUpdated(evt, _clock.GetCurrentInstant());
    public void Handle(AccountsServiceHeartbeatEvt evt)
    {
        foreach (var sid in _heartbeatStrategies)
        {
            var context = CreateStrategyContext(sid, false, evt.Timestamp, _clock.GetCurrentInstant());
            if (!context.StrategyRecord!.Status.IsActive()) continue;
            
            if (!context.IsOk)
            {
                _logger.LogWarning(
                    "Strategy {sid} failed to create context for heartbeat, error={error}",
                    sid, context.GetMissingInfoLogString()
                );
                continue;
            }
            var strategy  = _strategies[sid];
            strategy.Context = context;
            strategy.ProcessHeartbeat();
        }
    }
    
    private HostedStrategyCalculationContext CreateStrategyContext(int strategyId, bool isHistory, Instant referenceDt, Instant processingDt)
    {
        var strategy = _queryBus.Query<GetStrategyRecord, Strategy?>(new(strategyId));
        return new CalculationContext(
            _queryBus.Query<GetStrategyRecord, Strategy?>(new(strategyId)),
            _queryBus.Query<GetStrategyState, IStrategyStateReadonly?>(new(strategyId)),
            _queryBus.Query<GetAccount, IAccountStateReadonly?>(new(_accountByStrategy[strategyId].AccountId)),
            _queryBus.Query<GetStrategy, IStrategy?>(new(strategyId)),
            _queryBus.Query<GetAccount, ITradingAccount?>(new(_accountByStrategy[strategyId].AccountId)),
            _queryBus.Query<GetAccount, AccountRecordV6?>(new(_accountByStrategy[strategyId].AccountId)),
            referenceDt,
            processingDt,
            isHistory,
            _queryBus,
            _config.ThrowOnZeroVolumeOrders,
            _config.VirtualAccountSizeStepFraction
        );
    }

    private ExecutionContext CreateExecutionContext(int esaId, Instant processingDt) =>
        throw new NotImplementedException();

    public void ClaimHeartbeats(int? strategyId = null)
    {
        if (strategyId.HasValue) _heartbeatStrategies.Add(strategyId.Value);
    }
}