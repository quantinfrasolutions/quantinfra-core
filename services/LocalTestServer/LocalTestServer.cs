using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.StaticData.Abstractions;
using Common.Utils.Reflection;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Targets;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using QuantInfra.Backtesting.LocalMarketDataStorage;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.Strategies;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Services.BacktestingCore.Executor;
using QuantInfra.Services.BacktestingCore.Providers;

namespace QuantInfra.Services.LocalTestServer;

public class LocalTestServer : ITestServer, ITypeResolver
{
    private readonly IStaticDataProvider _sdProvider;
    private readonly ITestResultsPersister _resultsPersister;
    private readonly LocalTestServerConfig _config;
    private readonly ITestUnitsRepository _testUnitsRepository;
    private readonly ILoggerFactory _loggerFactory;
    private readonly LoggingConfiguration _logConfiguration;
    private readonly List<Assembly> _strategyAssemblies;
    private readonly HostedStrategiesFactory _factory;
    private readonly ILogger<LocalTestServer> _logger;
    
    private Dictionary<string, IStrategyTestAction?> _actions = new();
    private Dictionary<string, IMetricsCalculator> _metricsCalculators = new();

    public LocalTestServer(
        LocalTestServerConfig config,
        ITestUnitsRepository testUnitsRepository,
        IStaticDataProvider sdProvider,
        ITestResultsPersister resultsPersister,
        ILoggerFactory loggerFactory,
        LoggingConfiguration logConfiguration
    )
    {
        _sdProvider = sdProvider;
        _resultsPersister = resultsPersister;
        _config = config;
        _testUnitsRepository = testUnitsRepository;
        _loggerFactory = loggerFactory;
        _logConfiguration = logConfiguration;
        _logger = loggerFactory.CreateLogger<LocalTestServer>();

        PluginLoadContext.PreloadQuantInfraAssemblies();
        
        _strategyAssemblies = config.StrategyAssembliesPaths
            .Select(path =>
            {
                var loadContext = new PluginLoadContext(path);
                var assembly = loadContext.LoadFromAssemblyName(new(Path.GetFileNameWithoutExtension(path)));
                PluginLoadContext.AssertCompatible(assembly);
                return assembly;
            })
            .ToList();
        _factory = new HostedStrategiesFactory(this, loggerFactory);

        LoadPluginAssemblies(config);
    }

    
    public Type? ResolveType(string name)
    {
        foreach (var a in _strategyAssemblies)
        {
            var t = a.GetType(name);
            if (t != null) return t;
        }
        return null;
    }

    public IEnumerable<Assembly> LoadedStrategyAssemblies => _strategyAssemblies;

    private IMarketDataStorage GetMarketDataStorage()
    {
        return new Storage(new()
        {
            MarketDataPaths = _config.MarketDataPaths,
            DateTimeFormat = _config.DateTimeFormat,
        });
    }

    public Task<IReadOnlyCollection<string>> GetSupportedActionsAsync() =>
        Task.FromResult((IReadOnlyCollection<string>)_actions.Keys.ToList());

    public Task<IReadOnlyCollection<string>> GetSupportedMetricsCalculatorsAsync() =>
        Task.FromResult((IReadOnlyCollection<string>)_metricsCalculators.Keys.ToList());

    public Task<IReadOnlyCollection<StrategyTypeDescription>> GetStrategiesAsync() =>
        Task.FromResult<IReadOnlyCollection<StrategyTypeDescription>>(_factory.SupportedStrategyClasses.ToList());

    public Task<string> GetSampleActionParamsAsync(string actionName)
    {
        var action = _actions.GetValueOrDefault(actionName);
        if (action is null) throw new ArgumentException($"Action {actionName} not found");
        return Task.FromResult(action.GetSampleParams());
    }

    public Task<string> GetSampleMetricsCalculatorOptionsAsync(string calculator)
    {
        var calc = _metricsCalculators.GetValueOrDefault(calculator);
        if (calc is null) throw new ArgumentException($"Calculator {calculator} not found");
        return Task.FromResult(calc.GetSampleOptions());
    }

    public async Task<IReadOnlyCollection<RequiredMarketDataUnit>> ValidateRequiredMarketDataAsync(Guid unitId)
    {
        var unit = await _testUnitsRepository.GetTestUnitAsync(unitId);
        if (unit is null) throw new ArgumentException($"Test unit {unitId} not found");
        
        var action = _actions.GetValueOrDefault(unit.Action);
        if (action is null) throw new ArgumentException($"Action {unit.Action} not found");
        
        var requiredMarketData = action.GetMarketDataRequirements(unit);
        var reqs = await ValidateRequiredMarketDataInternal(unit.Options, unit.ContractOverride, requiredMarketData);
        return reqs.mdUnits;
    }

    public async Task<ActionParamsValidationResult> ValidateParamsAsync(string actionName, string? options)
    {
        if (!_actions.TryGetValue(actionName, out var action) || action is null)
        {
            return new(false, "Action not found");
        }
        return action.ValidateParams(options, _factory.SupportedStrategyClasses.Select(s => s.FullName).ToHashSet());
    }

    private async Task<(IReadOnlyCollection<RequiredMarketDataUnit> mdUnits, IReadOnlyCollection<int> fxConversionContracts, IReadOnlyCollection<ConstantStreamValue> constantStreams)> 
        ValidateRequiredMarketDataInternal(TestExecutorOptions options, ContractOverride? contractOverride, IReadOnlyCollection<MarketDataRequirement> md)
    {
        List<RequiredMarketDataUnit> reqs = new();
        
        var contractIds = md.SelectMany(x => x.ContractIds).Distinct().ToList();
        
        var fxConversions = md.SelectMany(
                s => contractIds.Select(c => new { ContractId = c, AccountCcyId = s.AccountCurrencyId })
            )
            .GroupBy(s => s.ContractId)
            .ToDictionary(
                g => g.Key, 
                g => g.Select(s => s.AccountCcyId).Distinct().ToHashSet()
            );
        
        var streamIds = md.SelectMany(s => s.StreamIds).Distinct().ToList();
    
        HashSet<int> fxConversionContracts = new();
        List<ConstantStreamValue> constantStreams = new();
        
        await ProcessContracts(options, contractIds, reqs, fxConversions, fxConversionContracts, constantStreams, contractOverride);
        
        
        streamIds = streamIds.Distinct().ToList();
        var streams = (await Task.Run(() => _sdProvider.GetStreams(streamIds))).ToDictionary(s => s.StreamId);
        reqs.AddRange(streamIds.Select(s =>
        {
            var stream = streams.GetValueOrDefault(s);
            if (stream == null) return new RequiredMarketDataUnit { StreamId = s, IsOk = false, Message = "Stream not found" };
            
            return new RequiredMarketDataUnit() { StreamId = s, StreamTicker = stream.Ticker, IsOk = true };
        }));
    
        var storage = GetMarketDataStorage();
        var res = await storage.ValidateRequiredMarketData(reqs, options.CandlesTimeframe);
        return (res, fxConversionContracts,  constantStreams);
    }
    
    private async Task ProcessContracts(TestExecutorOptions options, IReadOnlyCollection<int> contractIds,
        List<RequiredMarketDataUnit> results, Dictionary<int, HashSet<int>> contractToAccountsCcy,
        HashSet<int> fxConversionContracts, List<ConstantStreamValue> constantStreams, ContractOverride? contractOverride)
    {
        var contracts = (await Task.Run(() => _sdProvider.GetContracts(contractIds))).ToDictionary(c => c.ContractId);
        
        var notExistingContracts = contractIds.Except(contracts.Keys).ToList();
        if (notExistingContracts.Any())
        {
            if (contractOverride is not null)
            {
                var currency = await Task.Run(() => _sdProvider.GetCurrency(840));
                var template = GetTemplateForContractOverride(currency!, contractOverride);
                foreach (var cid in notExistingContracts)
                {
                    contracts.Add(cid, new(cid, $"{cid}", template, null, null, null, null, null, null, null, null,
                        [new() { DatafeedId = -1, StreamId = cid, Ticker = $"{cid}"}], -1));
                }
            }
            else
            {
                foreach (var cid in notExistingContracts)
                {
                    results.Add(new RequiredMarketDataUnit { ContractId = cid, IsOk = false, Message = "Contract does not exist" });
                }
            }
        }

        HashSet<int> missingContracts = new();
        
        foreach (var c in contracts.Values)
        {
            if (c.IsSynthetic())
            {
                var actualCompositions = c.SyntheticContractCompositionHistory!
                    .OrderByDescending(sc => sc.ValidFrom ?? Instant.MaxValue)
                    .Where(sc => sc.ValidFrom <= options.EndDt)
                    .ToList();
                var lastRequiredIndex = actualCompositions.FindIndex(sc => sc.ValidFrom <= options.StartDt);
                if (lastRequiredIndex > 0 && lastRequiredIndex < actualCompositions.Count)
                {
                    actualCompositions.RemoveRange(lastRequiredIndex, actualCompositions.Count);
                }
                var underlyingContractIds = actualCompositions.SelectMany(sc => sc.Weights.Keys).Distinct().ToList();
                foreach (var cid in underlyingContractIds.Except(contractIds))
                {
                    missingContracts.Add(cid);
                }

                // For each account currency mapped to the synthetic contract, add all its underlyings to the same currency
                if (contractToAccountsCcy.TryGetValue(c.ContractId, out var accountCcys))
                { 
                    foreach (var und in underlyingContractIds)
                    {
                        contractToAccountsCcy.TryAdd(und, new());
                        foreach (var ccyId in accountCcys)
                        {
                            contractToAccountsCcy[und].Add(ccyId);
                        }
                    }
                }
            }
            else
            {
                if (contractToAccountsCcy.TryGetValue(c.ContractId, out var accountCcys))
                {
                    foreach (var ccyId in accountCcys)
                    {
                        if (ccyId == c.Template.SettlementCurrency.CurrencyId) continue;
                        var conversionContractId = await Task.Run(() => 
                            _sdProvider.GetFxConversionContract(c.Template.SettlementCurrency.CurrencyId, ccyId).contractId);
                        if (!contractIds.Contains(conversionContractId)) missingContracts.Add(conversionContractId);
                        fxConversionContracts.Add(conversionContractId);
                    }
                }
                
                if (c.DefaultStream != null)
                {
                    if (results.All(r => r.StreamId != c.DefaultStream.StreamId))
                    {
                        results.Add(new RequiredMarketDataUnit
                        {
                            ContractId = c.ContractId, Ticker = c.Ticker, StreamId = c.DefaultStream.StreamId, IsOk = true,
                            DataRequired = c.DefaultStream.ConstantStreamValue is null,
                            Message = c.DefaultStream.ConstantStreamValue is not null ? "Constant value" : string.Empty,
                        });
                        if (c.DefaultStream.ConstantStreamValue is not null) constantStreams.Add(c.DefaultStream.ConstantStreamValue);
                    }
                }
                else
                {
                    results.Add(new RequiredMarketDataUnit
                    {
                        ContractId = c.ContractId, Ticker = c.Ticker, IsOk = false,
                        Message = "No stream configured for the contract"
                    });
                }
            }
        }
        
        if (missingContracts.Any()) await ProcessContracts(options, missingContracts, results,  contractToAccountsCcy, 
            fxConversionContracts, constantStreams, contractOverride);
    }

    private readonly SemaphoreSlim _executionSemaphore = new(1); // TODO: add concurrency to config

    public async Task RunAsync(Guid unitId)
    {
        await _executionSemaphore.WaitAsync();

        try
        {
            var unit = await _testUnitsRepository.GetTestUnitAsync(unitId);

            if (unit is null || (unit.Status != TestUnitStatus.Queued && unit.Status != TestUnitStatus.Failed))
            {
                _logger.LogInformation($"Unit {unitId} does not exist or is not failed or queued");
                _executionSemaphore.Release();
                return;
            }

            var action = _actions.GetValueOrDefault(unit.Action);
            if (action is null)
            {
                await _testUnitsRepository.SetUnitStatus(unitId, TestUnitStatus.Failed, "Invalid action");
                _executionSemaphore.Release();
                return;
            }
            
            IMetricsCalculator? calculator = null;
            if (!string.IsNullOrEmpty(unit.MetricsCalculatorName))
            {
                calculator = _metricsCalculators.GetValueOrDefault(unit.MetricsCalculatorName);
                if (calculator is null)
                {
                    await _testUnitsRepository.SetUnitStatus(unitId, TestUnitStatus.Failed, "Invalid metrics calculator");
                    _executionSemaphore.Release();
                    return;
                }
            }

            await _testUnitsRepository.SetUnitStatus(unitId, TestUnitStatus.Running);

            var requiredMarketData = action.GetMarketDataRequirements(unit);
            var reqs = await ValidateRequiredMarketDataInternal(unit.Options, unit.ContractOverride, requiredMarketData);

            var testSdProvider = await GetTestStaticDataProvider(unit.ContractOverride, requiredMarketData, reqs.mdUnits, reqs.fxConversionContracts, reqs.constantStreams);
            var contractTradingSessions = reqs.mdUnits
                .Where(c => c.ContractId.HasValue)
                .ToDictionary(
                    r => r.ContractId!.Value,
                    r => (IReadOnlyCollection<TradingSession>)testSdProvider.GetContract(r.ContractId!.Value).Template.TradingSessions.ToList()
                );

            var storage = GetMarketDataStorage();
            
            var candlesStorage = new InMemoryCandlesStorage(
                new()
                {
                    StartDt = unit.Options.StartDt,
                    EndDt = unit.Options.EndDt,
                    StorageTimeframe = unit.Options.CandlesTimeframe.ToDuration(),
                    UseCache = false,
                },
                storage.CreateMarketDataHistoryProvider(reqs.mdUnits, contractTradingSessions, unit.Options.CandlesTimeframe),
                _loggerFactory.CreateLogger<InMemoryCandlesStorage>()
            );

            // Adjust logging
            LoggingConfiguration loggingConfiguration = new();
            var target = _logConfiguration.FindTargetByName("test");
            if (target is FileTarget fileTarget)
            {
                fileTarget.FileName = Path.Combine(_config.WorkingDirectory, unit.TestId.ToString(), "test.log");
                var rule = _logConfiguration.FindRuleByName("test");
                if (rule is not null)
                {
                    rule.SetLoggingLevels(NLog.LogLevel.FromOrdinal((int)unit.Options.LogLevel), NLog.LogLevel.Off);
                    loggingConfiguration.AddRule(rule);
                }
                loggingConfiguration.AddTarget(fileTarget);
                foreach (var r in _logConfiguration.LoggingRules)
                {
                    if (r.RuleName == "test") continue;
                    loggingConfiguration.AddRule(r);
                }
            }


            var teFactory = new TestExecutorFactory(unit.Options, unit.PersistOptions, candlesStorage, testSdProvider, loggingConfiguration, _factory);

            var tracker = new ProgressTracker(unitId, _testUnitsRepository);
            action.Run(unit, teFactory, tracker, calculator, _resultsPersister);
            await _testUnitsRepository.SetUnitStatus(unitId, TestUnitStatus.Completed, $"Test time: {tracker.ExecutionTimeUs} ms");
        }
        catch (Exception ex)
        {
            await _testUnitsRepository.SetUnitStatus(unitId, TestUnitStatus.Failed, ex.Message);
        }
        finally
        {
            GC.Collect();
            GC.Collect();
            GC.Collect();
            _executionSemaphore.Release();
        }
    }
    

    private void LoadPluginAssemblies(LocalTestServerConfig config)
    {
        var actionAssemblies = config.PluginAssembliesPaths
            .Union(["QuantInfra.Services.BacktestingCore.dll"]) // add default actions and calculators
            .Select(path =>
            {
                var loadContext = new PluginLoadContext(path);
                var assembly = loadContext.LoadFromAssemblyName(new(Path.GetFileNameWithoutExtension(path)));
                PluginLoadContext.AssertCompatible(assembly);
                return assembly;
            })
            .ToList();

        _actions = actionAssemblies
            .SelectMany(a => a
                .GetTypes()
                .Where(t => 
                    t.IsClass
                    && typeof(IStrategyTestAction).IsAssignableFrom(t)
                    && !t.IsAbstract
                )
                .Select(t =>
                {
                    var instance = (IStrategyTestAction?)Activator.CreateInstance(t);
                    if (instance == null)
                    {
                        _logger.LogError($"Unable to create action {t.FullName}");
                        return (null, null);
                    }
                    return (instance.Name, instance);
                })
                .Where(x => x.instance != null)
            )
            .ToDictionary(x => x.Name, x => x.instance);
        
        _metricsCalculators = actionAssemblies
            .SelectMany(a => a
                .GetTypes()
                .Where(t => 
                    t.IsClass
                    && typeof(IMetricsCalculator).IsAssignableFrom(t)
                    && !t.IsAbstract
                )
                .Select(t =>
                {
                    var instance = (IMetricsCalculator?)Activator.CreateInstance(t);
                    if (instance == null)
                    {
                        _logger.LogError($"Unable to create calculator {t.FullName}");
                        return (null, null);
                    }
                    return (instance.Name, instance);
                })
                .Where(x => x.instance != null)
            )
            .ToDictionary(x => x.Name, x => x.instance);
    }
    
    private async Task<TestStaticDataRepository> GetTestStaticDataProvider(ContractOverride? contractOverride,
        IReadOnlyCollection<MarketDataRequirement> requiredMarketData, IReadOnlyCollection<RequiredMarketDataUnit> reqs,
        IReadOnlyCollection<int> fxConversionContracts,
        IReadOnlyCollection<ConstantStreamValue> constantStreams
    )
    {
        var testSdProvider = new TestStaticDataRepository();
        var contractIds = reqs
            .Where(r => r.ContractId.HasValue)
            .Select(r => r.ContractId!.Value)
            .ToList();

        ContractTemplate? overrideTemplate = null;
        var usd = await Task.Run(() => _sdProvider.GetCurrency(840));
        if (contractOverride is not null) overrideTemplate = GetTemplateForContractOverride(usd!, contractOverride);

        var contracts = (await Task.Run(() => _sdProvider.GetContracts(contractIds))).ToDictionary(c => c.ContractId);
        foreach (var cid in contractIds)
        {
            Contract? contract = null;

            if (contracts.TryGetValue(cid, out var c))
            {
                if (contractOverride?.OverrideAllContracts == true)
                {
                    var template = new ContractTemplate(c.Template.TemplateId, c.Template.Name,
                        c.Template.SecurityType, c.Template.Asset, c.Template.MinSize, c.Template.MinSizeMoney,
                        c.Template.MaxSize, c.Template.MaxSizeMoney, c.Template.SizeIncrement, c.Template.TickSize,
                        c.Template.TickValue, c.Template.PriceQuotation, c.Template.SettlementCurrency,
                        c.Template.PnLCalculatorType, c.Template.BaseCurrency, c.Template.QuoteCurrency,
                        c.Template.DefaultDatafeed, 
                        [new CommissionStructure
                        {
                            CommissionId = -1, 
                            CommissionStructureType = CommissionStructureType.Other,
                            Currency = usd,
                            FixedPerShare = contractOverride.CostPerShare,
                            Floating = contractOverride.FloatingCost,
                        }], 
                        c.Template.TradingSessions, c.Template.Exchange, c.Template.Broker, 
                        c.Template.DaysInYear, c.Template.Description);

                    contract = new(c.ContractId, c.Ticker, template, c.FirstTradingDate, c.ExpirationDate,
                        c.SyntheticContractType,
                        c.SynthRequiresBarRecalculationAtRollover, c.SyntheticContractCompositionHistory,
                        c.ExternalContractId,
                        c.Asset, c.Description, c.Streams, c.DefaultDatafeedId);
                }
                else contract = c;
            }
            else if (overrideTemplate is not null)
            {
                contract = new(cid, $"{cid}", overrideTemplate, null, null,
                    null, null, null, null,
                    new() { AssetId = cid },
                    null,
                    [new() { DatafeedId = -1, StreamId = cid, Ticker = $"{cid}" }], -1);
            }
            
            if (contract is null) throw new Exception($"Unable to find contract with id {cid}, and no override was provided");
            testSdProvider.TryAddContract(contract);
            if (fxConversionContracts.Contains(contract.ContractId)) testSdProvider.TryAddFxConversionContract(contract);
        }

        // trading sessions exist in contracts
        // var tradingSessionIds = strategies
        //     .SelectMany(s =>
        //         s.RequiredBarStorages.SelectMany(bs =>
        //             bs.Value.TradingSessionIds ?? Array.Empty<int>()
        //         )
        //     )
        //     .Distinct()
        //     .Except(contracts.SelectMany(c => c.Template.TradingSessions.Select(ts => ts.TradingSessionId)).Distinct())
        //     .ToList();
        //
        // foreach (var tsId in tradingSessionIds)
        // {
        //     testSdProvider.TryAddTradingSession(sdProvider.GetTradingSession(tsId));
        // }

        foreach (var csv in constantStreams)
        {
            testSdProvider.TryAddConstantStreamValue(csv);
        }
        
        var currencies = contracts.Values.Select(c => c.Template.SettlementCurrency)
            .Union(contracts.Values.Where(c => c.Template.BaseCurrency is not null).Select(c => c.Template.BaseCurrency))
            .Union(contracts.Values.Where(c => c.Template.QuoteCurrency is not null).Select(c => c.Template.QuoteCurrency))
            .ToList();
        foreach (var currency in currencies)
        {
            if (currency is not null) testSdProvider.TryAddCurrency(currency);
        }
        var missingCurrencyIds = requiredMarketData.Select(s => s.AccountCurrencyId)
            .Except(currencies.Select(c => c.CurrencyId)).Distinct().ToList();
        foreach (var ccyId in missingCurrencyIds)
        {
            testSdProvider.TryAddCurrency(_sdProvider.GetCurrency(ccyId)!);
        }

        return testSdProvider;
    }
    
    private ContractTemplate GetTemplateForContractOverride(Currency usd, ContractOverride contractOverride) =>
        new(-1, "Override", contractOverride.SecurityType, null, 
            contractOverride.MinSize, contractOverride.MinSizeMoney,
            contractOverride.MaxSize, contractOverride.MaxSizeMoney,
            contractOverride.SizeIncrement, contractOverride.TickSize, contractOverride.TickValue, 1, usd!,
            contractOverride.PnLCalculatorType, null, null,
            new Datafeed() { DatafeedId = -1 },
            [
                new CommissionStructure()
                {
                    CommissionId = -1,
                    CommissionStructureType = CommissionStructureType.Other,
                    FixedPerShare = contractOverride.CostPerShare,
                    Floating = contractOverride.FloatingCost,
                    Currency = usd,
                }
            ],
            Array.Empty<TradingSession>(),
            new Exchange() { ExchangeId = -1 },
            new Broker() { BrokerId = -1 },
            252,
            null
        );
    
    private static readonly Lazy<JsonSerializerOptions> Options = new(() =>
    {
        var options = new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
            WriteIndented = false,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        options.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    });

    public static JsonSerializerOptions JsonSerializerOptions => Options.Value;
}