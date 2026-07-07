using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Common.StaticData.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Common.Strategies;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Domain.Accounts.Base;
using QuantInfra.Domain.Commands.Accounts.AccountsService;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.HostedStrategies;
using QuantInfra.Domain.MarketData;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Domain.StaticData;
using QuantInfra.Domain.VirtualExecution;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading;
using QuantInfra.Services.BacktestingCore.Providers;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace QuantInfra.Services.BacktestingCore.Executor;

public class TestExecutor: 
    IBacktestRunner
{
    private readonly TestExecutorFactory _factory;
    private readonly ServiceProvider _serviceProvider;
    private readonly IReadOnlyCollection<BacktestedStrategyConfig> _strategies;
    private readonly IActionProgressTracker? _progressTracker;
    private readonly BacktestResultsAgregator _results;
        
    public TestExecutor(
        TestExecutorFactory factory,
        TestExecutorOptions options,
        PersistOptions persistOptions,
        ITestMarketDataProvider candlesStorage,
        TestStaticDataRepository sdProvider,
        IReadOnlyCollection<BacktestedStrategyConfig> strategies,
        LoggingConfiguration logConfiguration,
        IHostedStrategiesFactory strategiesFactory,
        IActionProgressTracker? progressTracker
    )
    {
        _factory = factory;
        Options = options;
        _strategies = strategies;
        _progressTracker = progressTracker;

        var serviceCollection = new ServiceCollection()
            .AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddNLog();
                LogManager.Configuration = logConfiguration;
            })

            .AddSingleton<ITestMarketDataProvider>(candlesStorage)
            .AddSingleton<IMarketDataHistoryProvider>(sp => sp.GetRequiredService<ITestMarketDataProvider>())
                
            .AddSingleton<HostedStrategiesRunnerConfig>(_ => new HostedStrategiesRunnerConfig(Duration.Zero,
                options.RequestBarAttempts, options.ThrowOnZeroVolumeOrders, options.VirtualAccountSizeStepFraction, false))
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
            .AddSingleton<IHostedStrategiesFactory>(_ => strategiesFactory)
            .AddSingleton<TestStaticDataRepository>(_ => sdProvider)
            .AddSingleton<IStaticDataProvider>(_ => sdProvider)
                
            .UseVirtualExecutorWithSingletonHandlers()
                
            .AddStaticDataQueryHandlers()
            .AddLastContractPricesStorage()
            .AddBaseAccounts()
            .AddSingleton<Config>(_ => new() { LogLevel = options.LogLevel })
            .AddInMemoryState()
            .UseSingletonInMemoryBus()
            .AddBacktestResultsAggregator(options, persistOptions, strategies.Count);

        _serviceProvider = serviceCollection.BuildServiceProvider();
        _results = _serviceProvider.GetRequiredService<BacktestResultsAgregator>();
    }
        
    protected TestExecutorOptions Options { get; }
    public CancellationToken? Ct { get; set; }
    // public IActionProgressTracker Tracker { get; set; }


    public virtual void Run()
    {
        var eventBus = _serviceProvider.GetRequiredService<IEventBus>();
        var queryBus = _serviceProvider.GetRequiredService<IQueryBus>();
        var commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
        
        var sw = Stopwatch.StartNew();
        
        var now = Options.StartDt.Minus(Duration.Epsilon);
        foreach (var config in _strategies)
        {
            var strategyId = _factory.GetNewStrategyId();
            var account = new AccountRecordV6("bt", config.Name,
                config.AccountCurrencyId, AccountType.VirtualAccount,
                config.PositionAccounting, null, true,
                config.IncludeUnrealizedPnLToMtm, null, strategyId);
            eventBus.Emit(new AccountCreatedEvt(0, strategyId, account, now));
            _accounts.Add(account);
            commandBus.SendCommand(new ProcessBalanceOperationCmd(string.Empty, new()
            {
                AccountId = strategyId,
                Amount = Options.Investment,
                AssetId = account.CurrencyId,
            }));
            
            var strategy = new Strategy(strategyId, config.Name, config.ClassName, config.Params, config.RequiredBarStorages,
                config.Symbols, config.LiquidationParameters, config.UseSignalGroups, StrategyStatus.Running, strategyId, string.Empty);
            _strategyConfigs.Add(strategy);
            eventBus.Emit(new StrategyCreatedEvt(0, strategyId, strategy, account, now));
        }

        now = Options.StartDt;
            
        commandBus.SendCommand(new RunEndOfDayCmd(string.Empty, now));

        var ve = _serviceProvider.GetRequiredService<VirtualExecutor>();
        ve.Initialize(queryBus);

        var state = _serviceProvider.GetRequiredService<InMemoryState>();
        var runner = _serviceProvider.GetRequiredService<HostedStrategiesRunner>();
        runner.Initialize(state.StrategyRecords.Values.ToList(), state.AccountRecords.Values.ToList());
            
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
        // var btsInitialized = false;
        var nextMtmDt = GetNextMtmDt(Options.StartDt);
        // var totalSecondsToTest = (Options.EndDt - Options.StartDt).TotalSeconds;
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

            if (dt >= nextMtmDt)
            {
                commandBus.SendCommand(new RunEndOfDayCmd(string.Empty, nextMtmDt));
                nextMtmDt = GetNextMtmDt(nextMtmDt);
                _progressTracker?.SetCurrentProgress((dt - Options.StartDt).TotalSeconds / (Options.EndDt - Options.StartDt).TotalSeconds);
            }
                
            Contract? contract = null;
            if (bar.ContractId.HasValue)
            {
                contract = queryBus.Query<GetContract, Contract?>(new(bar.ContractId.Value));
                    
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

        var elapsed = sw.Elapsed;
        _progressTracker.SetTestExecutionTime(sw.ElapsedMilliseconds);
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


    public StrategyTestResult GetResult() => new(
        _strategyConfigs,
        _accounts,
        _results.Returns,
        _results.Trades,
        _results.PositionCloses,
        _results.EndOfDayPositions,
        null, // TODO
        _results.EndOfDayBalances,
        _results.PositionValues,
        Array.Empty<Commission>()
    );
    
    private readonly List<Strategy> _strategyConfigs = new();
    // public IReadOnlyCollection<BacktestedStrategyConfig> GetStrategyConfigs() => _strategyConfigs;
    //
    private readonly List<AccountRecordV6> _accounts = new();
    // public IReadOnlyCollection<AccountRecordV6> GetAccountRecords() => _accounts;
    //
    // public IReadOnlyList<Commission> GetCommissions() => throw new NotImplementedException();// Results.Commissions;
    // public IReadOnlyDictionary<int, IReadOnlyList<SharePriceHistory>> GetReturns() => _results.Returns;
    // public IReadOnlyList<Position> GetPositionCloses() => _results.PositionCloses;
    // public IReadOnlyList<Position> GetEndOfDayPositions() => _results.EndOfDayPositions;
    // public IReadOnlyList<BalanceValue> GetEndOfDayBalances() => _results.EndOfDayBalances;
    // public IReadOnlyList<PositionValue> GetPositionValues() => _results.PositionValues;
    // public IReadOnlyList<Trade> GetTrades() => _results.Trades;
}