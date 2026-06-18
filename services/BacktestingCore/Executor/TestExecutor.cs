using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BacktestingCore.Analysis;
using BacktestingCore.Providers;
using Common.Accounting;
using Common.Accounting.Yield;
using Common.Backtesting;
using Common.EventSourcing;
using Common.Profiling;
using Common.Strategies;
using Common.Strategies.Runner;
using Common.Trading;
using Common.Trading.Positions;
using Domain.Commands.Accounts.AccountsService;
using Domain.Queries.Accounts.AccountsService;
using Domain.StaticData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Common.StaticData.Abstractions;
using QuantInfra.Common.Strategies;
using QuantInfra.Common.Strategies.Api;
using QuantInfra.Domain.Accounts.Base;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.HostedStrategies;
using QuantInfra.Domain.MarketData;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Domain.VirtualExecution;

namespace BacktestingCore.Executor
{
    public class TestExecutor: 
        IStrategyTestingAction
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IReadOnlyCollection<CreateStrategyRequest> _strategies;
        private readonly BacktestResultsAgregator _results;
        
        private int _strategyId = 10000;
        
        private double _progress;
        
        public TestExecutor(
            TestExecutorOptions options,
            ITestMarketDataProvider candlesStorage,
            TestStaticDataRepository sdProvider,
            IReadOnlyCollection<CreateStrategyRequest> strategies,
            LoggingConfiguration logConfiguration,
            IProfiler profiler,
            IHostedStrategiesFactory strategiesFactory
        )
        {
            Options = options;
            _strategies = strategies.ToList();

            var serviceCollection = new ServiceCollection()
                .AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddNLog();
                    LogManager.Configuration = logConfiguration;
                })

                .AddSingleton<ITestMarketDataProvider>(candlesStorage)
                .AddSingleton<IMarketDataHistoryProvider>(sp => sp.GetRequiredService<ITestMarketDataProvider>())
                
                .AddSingleton<HostedStrategiesRunnerConfig>(sp => Options)
                .AddHostedStrategiesRunner()

                // .AddSingleHostMessaging()
                // .UseInMemoryStrategiesRepository()
                .AddSingleton<IMarketDataHistoryProvider>(sp =>
                    sp.GetService<ITestMarketDataProvider>()!
                )
                .AddSingleton<IClock>(sp => sp.GetService<ITestMarketDataProvider>()!)
                // .AddSingleton<ITypeResolver>(
                //     strategiesFactory.
                //     //new SingleAssemblyTypeResolver(Assembly.Load("Strategies"))
                // )
                .AddSingleton<IHostedStrategiesFactory>(sf => strategiesFactory)
                .AddSingleton<TestStaticDataRepository>(sp => sdProvider)
                .AddSingleton<IStaticDataProvider>(sp => sdProvider)
                .AddSingleton<IProfiler>(sp => profiler)
                
                .UseVirtualExecutorWithSingletonHandlers()
                .AddSingleton<HostedStrategiesRunnerConfig>(sp => Options)
                
                .AddStaticDataQueryHandlers()
                .AddLastContractPricesStorage()
                .AddBaseAccounts()
                .AddInMemoryState()
                .UseSingletonInMemoryBus()
                .AddBacktestResultsAggregator(options, strategies.Count);

            _serviceProvider = serviceCollection.BuildServiceProvider();
            _results = _serviceProvider.GetRequiredService<BacktestResultsAgregator>();

            // var bus = _serviceProvider.GetService<BacktestingBus>()!;
            // bus.InitializeHandlers(_serviceProvider);

            // _serviceProvider.GetService<StrategiesRunner>().CurrentPricesProvider =
            //     _serviceProvider.GetService<BarsRunner>();
        }
        
        protected TestExecutorOptions Options { get; }
        public CancellationToken? Ct { get; set; }
        public IActionProgressTracker Tracker { get; set; }


        public virtual void Run()
        {
            #if PROFILE
            var profiler = _serviceProvider.GetService<IProfiler>();
            #endif
            var eventBus = _serviceProvider.GetRequiredService<IEventBus>();
            var queryBus = _serviceProvider.GetRequiredService<IQueryBus>();
            var commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
            
            var now = Options.StartDt.Minus(Duration.Epsilon);
            foreach (var config in _strategies)
            {
                var strategyId = _strategyId++;
                var currency = queryBus.Query<GetCurrency, Currency>(new(config.Account.CurrencyId));
                var account = new AccountRecordV6(config.Account.AccountServiceName, config.Account.Name,
                    config.Account.CurrencyId, config.Account.AccountType,
                    config.Account.PositionAccounting, config.Account.BrokerId, config.Account.EnableSharePriceTracking,
                    config.Account.IncludeUnrealizedPnLToMtm, null, strategyId);
                eventBus.Emit(new AccountCreatedEvt(0, strategyId, account, now));
                _accounts.Add(account);
                commandBus.SendCommand(new ProcessBalanceOperationCmd(string.Empty, new()
                {
                    AccountId = strategyId,
                    Amount = Options.Investment,
                    AssetId = account.CurrencyId,
                }));

                var sc = config.ToStrategyConfig(strategyId);
                _strategyConfigs.Add(sc);
                var strategy = new Strategy(sc.StrategyId, sc.Name, sc.ClassName, sc.Params, sc.RequiredBarStorages,
                    sc.Symbols, sc.LiquidationParameters, sc.UseSignalGroups, StrategyStatus.Running, strategyId, string.Empty);
                eventBus.Emit(new StrategyCreatedEvt(0, strategyId, strategy, account, now));
            }

            now = Options.StartDt;
            
            commandBus.SendCommand(new RunEndOfDayCmd(string.Empty, now));

            var ve = _serviceProvider.GetRequiredService<VirtualExecutor>();
            ve.Initialize(queryBus);

            var state = _serviceProvider.GetRequiredService<InMemoryState>();
            var runner = _serviceProvider.GetRequiredService<HostedStrategiesRunner>();
            runner.Initialize(state.StrategyRecords.Values.ToList(), new List<EsaSubscription>(), state.AccountRecords.Values.ToList());
            
            var candlesStorage = _serviceProvider.GetService<ITestMarketDataProvider>()!;
            runner.LoadMarketData(Options.StartDt, candlesStorage, true);
            
//             var csf = _serviceProvider.GetService<CalculationServiceFactory>();
//             var candlesStorage = _serviceProvider.GetService<ITestMarketDataProvider>()!;
//             
//             csf.Initialize(false, clock.GetCurrentInstant(), strategiesRepository, candlesStorage, sdProvider, queryBus, eventBus, forceLoadBaus: true);
//             
//             var runner = csf.GetInstance()!.Value.Runner;
//             

            var clock = _serviceProvider.GetRequiredService<IClock>();
            #if PROFILE
            using (profiler.Step("ProcessBars"))
            {
            #endif
            var btsInitialized = false;
            var nextMtmDt = GetNextMtmDt(Options.StartDt);
            var totalSecondsToTest = (Options.EndDt - Options.StartDt).TotalSeconds;
            Instant dt = Options.StartDt;
            do
            {
                if (Ct?.IsCancellationRequested == true) return;

                var sdRepository = _serviceProvider.GetRequiredService<TestStaticDataRepository>();
                foreach (var cs in sdRepository.ConstantStreams.Values)
                {
                    var cc = sdRepository.GetContractForConstantStream(cs.StreamId);
                    if (cc is null) continue;
                    
                    var price = cc.NormalizePrice(cs.Value); 
                    eventBus.Emit(new ContractLastPriceUpdatedEvt(
                        cc.ContractId, 
                        price, 
                        null,
                        dt, 
                        dt
                    ));
                }
                
                var bar = candlesStorage.GetNextBar();
                if (bar == null) break;
                
                dt = bar.OpenDt;
                
                if (/*!Options.EnableAutomaticBaseTradeSizeManagement &&*/ !btsInitialized) // otherwise, updates will be handled by BarsRunner
                {
                    // TODO: support for multiple symbols and synthetics
                    btsInitialized = true;
                    // sdProvider.UpdateBaseTradeSize(
                    //     Options.ContractId,
                    //     bar.CloseDt,
                    //     Options.InvestmentEquivalent / sdProvider.GetCalculator(Options.ContractId)
                    //         .GetValueInSettlementCcy((decimal)bar.Open, 1),
                    //     (decimal)bar.Open,
                    //     1
                    // );
                }

                if (dt >= nextMtmDt)
                {
                    commandBus.SendCommand(new RunEndOfDayCmd(string.Empty, nextMtmDt));
                    nextMtmDt = GetNextMtmDt(nextMtmDt);
                    if (Tracker is not null)
                    {
                        Tracker.CurrentProgress = (dt - Options.StartDt).TotalSeconds / totalSecondsToTest;
                    }
                }
                
                Contract? contract = null;
                if (bar.ContractId.HasValue)
                {
                    contract = queryBus.Query<GetContract, Contract>(new(bar.ContractId.Value));
                    
                    dt = bar.OpenDt.Plus(Options.OpenExecutionOffset);
                    
                    var price = contract.NormalizePrice(bar.Open); 
                    eventBus.Emit(new ContractLastPriceUpdatedEvt(
                        contract.ContractId, 
                        price, 
                        bar.TradingSessionId,
                        dt, 
                        dt
                    ));

                    if (Options.CheckOrdersAtBarOpen)
                    {
                        ve.CheckOrders(contract.ContractId, price, bar.TradingSessionId, dt, dt, 
                            queryBus, eventBus, Options.StopOrdersExecution);
                    }
                }
                
                if (Options.CheckPendingOrdersExecutionUsingHighLow && contract != null)
                {
                    dt = bar.OpenDt.Plus(Options.HighLowExecutionOffset);
                    decimal? executionPrice = Options.StopOrdersExecution == StopOrdersExecution.BarClose ? 
                        contract.NormalizePrice(bar.Close) : null;
                    ve.CheckPendingOrders(contract.ContractId, 
                        contract.NormalizePrice(bar.High), contract.NormalizePrice(bar.Low), 
                        bar.TradingSessionId, dt, dt, Options.StopOrdersExecution, eventBus, queryBus, executionPrice);
                }

                dt = bar.CloseDt;

                try
                {
                    runner.OnExchangeBar(bar, false, dt);
                }
                catch (ConfigurationChangedException ex)
                {
                    throw;
                    // csf.Initialize(false, clock.GetCurrentInstant(), strategiesRepository, candlesStorage, sdProvider, queryBus, eventBus, ex, true);
                    //
                    // runner = csf.GetInstance()!.Value.Runner;
                    // runner.OnExchangeBar(bar, clock.GetCurrentInstant(), sdProvider, queryBus, eventBus);
                }

                if (contract != null)
                {
                    var price = contract.NormalizePrice(bar.Close); 
                    eventBus.Emit(new ContractLastPriceUpdatedEvt(
                        contract.ContractId, 
                        price, 
                        bar.TradingSessionId,
                        dt, 
                        dt
                    ));
                    
                    if (Options.CheckOrdersAtBarClose)
                    {
                        ve.CheckOrders(contract.ContractId, price, bar.TradingSessionId, dt, dt, queryBus, eventBus,
                            Options.StopOrdersExecution, Options.LimitCloseCheckToMarketOrdersOnly);
                    }
                }
            } while (true);

            // Close all positions to account them in the final results
            now = clock.GetCurrentInstant();
            foreach (var strategyId in state.StrategyRecords.Values.Select(s => s.StrategyId))
            {
                // TODO
                // var strategy = queryBus.Query<GetStrategy, IStrategy>(new(strategyId));
                // strategy.Stop("Backtest ended");
                var accountState = state.AccountStates[strategyId];
                var account = queryBus.Query<GetAccount, IAccount?>(new(strategyId));

                foreach (var orders in accountState.Orders.ToList())
                {
                    account!.CancelOrder(
                        new() { AccountId = account.AccountId, OrderId = orders.OrderId, },
                        now
                    );
                }
                    
                foreach (var position in accountState.Positions.ToList())
                {
                    account!.PlaceOrder(
                        position.GetClosingOrder($"stop-{position.ContractId}-{position.StrategyPositionId}"), 
                        now
                    );
                }
            }

            now = now.Plus(Duration.Epsilon);

            
            foreach (var p in queryBus.Query<GetLastKnownContractPrices, IReadOnlyDictionary<int, decimal>>(new()))
            {
                ve.CheckOrders(p.Key, p.Value, null, now, now, queryBus, eventBus, Options.StopOrdersExecution, true);
            }
            
            // TODO: convert balances to account currency
            var remainingPositions = state.AccountStates.Values.SelectMany(a => a.Positions).ToList();
            if (remainingPositions.Any())
            {
                throw new Exception("Not all positions were closed");
            }

            commandBus.SendCommand(new RunEndOfDayCmd(string.Empty, nextMtmDt));
#if PROFILE
            }
#endif
        }

        private Instant GetNextMtmDt(Instant lastMtmDt)
        {
            var lastMtmLocal = lastMtmDt.InUtc().LocalDateTime;
            return
                new ZonedDateTime(
                        new LocalDateTime(lastMtmLocal.Year, lastMtmLocal.Month, lastMtmLocal.Day, 0, 0),
                        DateTimeZone.Utc, Offset.Zero
                    )
                    .ToInstant() // Get the UTC date of the last MTM
                    .Plus(Duration.FromDays(1)) // Add 1 day
                    .Plus(Options.MtmUtcOffset); // Add configured offset
        }

        private readonly List<StrategyConfig> _strategyConfigs = new();
        public IReadOnlyCollection<StrategyConfig> GetStrategyConfigs() => _strategyConfigs;

        private readonly List<AccountRecordV6> _accounts = new();
        public IReadOnlyCollection<AccountRecordV6> GetAccountRecords() => _accounts;

        public void PersistTestData(
            ITestDataPersister? persister = null, 
            PersistOptions? options = null,
            bool flush = true
        )
        {
            options ??= new PersistOptions();
            #if PROFILE
            var profiler = _serviceProvider.GetService<IProfiler>();
            using (profiler.Step("SaveResults"))
            {
            #endif
            // if (options.SaveStrategies) persister?.SaveStrategyConfigs(_strategies);
            // if (options.SaveDailyReturns) persister?.SaveDailyReturns(Results.Returns);
            // if (options.SavePositions) persister?.SavePositions(Results.PositionCloses);
            // if (options.SaveTrades) persister?.SaveTrades(Results.Trades);
            // if (options.SaveCommissions) persister?.SaveCommissions(Results.Commissions);
            // if (flush) persister?.Flush();
            #if PROFILE
            }
            #endif
        }
        
        public IReadOnlyList<Commission> GetCommissions() => throw new NotImplementedException();// Results.Commissions;
        public IReadOnlyDictionary<int, IReadOnlyList<SharePriceHistory>> GetReturns() => _results.Returns;
        public IReadOnlyList<Position> GetPositionCloses() => _results.PositionCloses;
        public IReadOnlyList<Position> GetEndOfDayPositions() => _results.EndOfDayPositions;
        public IReadOnlyList<BalanceValue> GetEndOfDayBalances() => _results.EndOfDayBalances;
        public IReadOnlyList<PositionValue> GetPositionValues() => _results.PositionValues;
        public IReadOnlyList<Trade> GetTrades() => _results.Trades;
    }
}