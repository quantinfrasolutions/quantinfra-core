using Domain.Accounts.Base.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.AccountsService.Projections;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Positions;
using QuantInfra.Tests.Mocks;
using GenericMockEventHandler = Domain.Accounts.Base.Tests.GenericMockEventHandler;
using MockIdsProvider = Domain.Accounts.Base.Tests.MockIdsProvider;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Domain.Accounts.Base.Tests;

public class PnLCalculationTests
{
    private const int AccountId = 100000;
    
    private AccountBase _accountUsd, _accountEur;
    private AccountBaseState _stateUsd, _stateEur;
    
    private GenericMockEventHandler _events;
    private MockProjectionHandler _projections;
    private MockQueryHandler<GetConversionRate, decimal?> _fxRateQueryHandler;
    private readonly FileClock _clock = new();
    private StaticData _data;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _events = new GenericMockEventHandler();
        _projections = new MockProjectionHandler();

        _data = new StaticData();

        _data.Contracts.Add(10000,
            new(
                10000,
                "STK",
                new ContractTemplate { SettlementCurrency = new Currency { CurrencyId = 840, }, SecurityType = SecurityType.Stock },
                null, null, null, null, null,
                "test-id",
                new Asset { AssetId = 100, Name = "STK" },
                null, new List<Stream>(),
                1
            )
        );
        
        _data.Contracts.Add(10001, 
            new(
                10001,
                "STK2",
                new ContractTemplate { SettlementCurrency = new Currency { CurrencyId = 978, }, SecurityType = SecurityType.Stock },
                null, null, null, null, null,
                "test-id-2",
                new Asset { AssetId = 100, Name = "STK2" },
                null, new List<Stream>(),
                1
            )
        );
        
        _data.Assets.Add(840, new() { AssetId = 840 });
        _data.Assets.Add(978, new() { AssetId = 978 });
        _data.Currencies.Add(840, new Currency { CurrencyId = 840, });
        _data.Currencies.Add(978, new Currency { CurrencyId = 978, });

        var serviceProvider = new ServiceCollection()
            .UseSingletonInMemoryBus()
            .AddLogging(c =>
            {
                c.ClearProviders();
                c.AddFilter("*", LogLevel.Debug).AddConsole();
            })
            .AddSingleton<IEventHandler>(_events)
            .AddSingleton<IQueryHandler<GetContract, Contract>>(_data)
            .AddSingleton<IQueryHandler<GetCurrency, Currency>>(_data)
            .AddSingleton<IQueryHandler<GetAsset, Asset?>>(_data)
            .AddSingleton<IQueryHandler<GetConversionRate, decimal?>>(_data)
            .AddSingleton<IQueryHandler<GetConversionPath, IReadOnlyCollection<FxConversionStep>>>(_data)
            .AddSingleton<IProjectionWriter>(_projections)
            
            .BuildServiceProvider();
        
        var accountRecord = new AccountRecordV6("AS", "Test account USD", 840, AccountType.VirtualAccount,
            PositionAccounting.Netted, null, true, true, null);

        _stateUsd = AccountBaseState.CreateNewState(accountRecord,
            serviceProvider.GetRequiredService<IEventBus>(), serviceProvider.GetRequiredService<ILoggerFactory>());

        _accountUsd = new AccountBase(
            accountRecord,
            _stateUsd,
            new MockIdsProvider(),
            new MockBOIdProvider(),
            new MockOrderIdProvider(),
            new MockExecIdProvider(),
            new MockTradeIdProvider(),
            serviceProvider.GetRequiredService<IEventBus>(),
            serviceProvider.GetRequiredService<IQueryBus>(),
            serviceProvider.GetRequiredService<ILoggerFactory>(),
            LogLevel.Debug
        );
        
        accountRecord = new AccountRecordV6("AS", "Test account EUR", 978, AccountType.VirtualAccount,
            PositionAccounting.Netted, null, true, true, null, 101);
        _stateEur = AccountBaseState.CreateNewState(accountRecord,
            serviceProvider.GetRequiredService<IEventBus>(), serviceProvider.GetRequiredService<ILoggerFactory>());
        
        _accountEur = new AccountBase(
            accountRecord,
            _stateEur,
            new MockIdsProvider(),
            new MockBOIdProvider(),
            new MockOrderIdProvider(),
            new MockExecIdProvider(),
            new MockTradeIdProvider(),
            serviceProvider.GetRequiredService<IEventBus>(),
            serviceProvider.GetRequiredService<IQueryBus>(),
            serviceProvider.GetRequiredService<ILoggerFactory>(),
            LogLevel.Debug
        );
    }
    
    [Test, Order(1)]
    public void Test1MakeInvestments()
    {
        _clock.Instant = Instant.FromUtc(2026, 2, 4, 13, 0);
        
        _data.SetRate(978, 840, 1.25m);
        _data.SetRate(840, 978, 0.8m);
        _accountUsd.ProcessBalanceOperation(new () { AccountId = _accountUsd.AccountId, AssetId = 840, Amount = 10000 }, _clock.GetCurrentInstant());
        Assert.That(_accountUsd.AccountStateReadonly.Investment, Is.EqualTo(10000));
        _accountEur.ProcessBalanceOperation(new () { AccountId = _accountEur.AccountId, AssetId = 840, Amount = 10000 }, _clock.GetCurrentInstant());
        Assert.That(_accountEur.AccountStateReadonly.Investment, Is.EqualTo(8000));
        
        _accountUsd.ProcessBalanceOperation(new() { AccountId = _accountUsd.AccountId, AssetId = 978, Amount = 10000 }, _clock.GetCurrentInstant());
        Assert.That(_accountUsd.AccountStateReadonly.Investment, Is.EqualTo(22500));
        _accountEur.ProcessBalanceOperation(new() { AccountId = _accountEur.AccountId, AssetId = 978, Amount = 10000 }, _clock.GetCurrentInstant());
        Assert.That(_accountEur.AccountStateReadonly.Investment, Is.EqualTo(18000));
    }

    [Test, Order(2)]
    public void Test2TradeUsdContract()
    {
        _clock.Instant += Duration.FromMinutes(1);
        _accountUsd.ProcessTrade(new("AS1", 100, null, _accountUsd.AccountId, 10000, null, 1000, null, null, null, Side.Buy, 10, 1000, 100, _clock.GetCurrentInstant(),
            null, null, null, 840, 1, 10000, null, null, false), _clock.GetCurrentInstant());
        _accountEur.ProcessTrade(new("AS1", 100, null, _accountEur.AccountId, 10000, null, 1000, null, null, null, Side.Buy, 10, 1000, 100, _clock.GetCurrentInstant(),
            null, null, null, 840, 0.8m, 10000, null, null, false), _clock.GetCurrentInstant());
        
        Assert.That(_accountUsd.AccountStateReadonly.Positions.Count, Is.EqualTo(1));
        var pos = _accountUsd.AccountStateReadonly.Positions.First();
        Assert.That(pos.SignedVolume, Is.EqualTo(10));
        Assert.That(pos.OpenPrice, Is.EqualTo(1000));
        Assert.That(pos.Commission, Is.EqualTo(100));
        Assert.That(pos.TotalSettlPaymentsInAccountCcy, Is.EqualTo(10000));
        
        Assert.That(_accountEur.AccountStateReadonly.Positions.Count, Is.EqualTo(1));
        pos = _accountEur.AccountStateReadonly.Positions.First();
        Assert.That(pos.SignedVolume, Is.EqualTo(10));
        Assert.That(pos.OpenPrice, Is.EqualTo(1000));
        Assert.That(pos.Commission, Is.EqualTo(100));
        Assert.That(pos.TotalSettlPaymentsInAccountCcy, Is.EqualTo(8000));
        
        
        _clock.Instant += Duration.FromMinutes(1);
        _accountUsd.ProcessTrade(new("AS1", 100, null, _accountUsd.AccountId, 10000, null, 1000, null, null, null, Side.Sell, 4, 1100, 44, _clock.GetCurrentInstant(),
            null, null, null, 840, 1, 4400, null, null, false), _clock.GetCurrentInstant());
        
        var ph = _projections.Projections
            .Where(p => p is PositionChangedEvt ph && ph.Type == PositionChangeType.Close && ph.AccountId == _accountUsd.AccountId)
            .Select(p => (PositionChangedEvt)p)
            .ToList();
        Assert.That(ph.Count, Is.EqualTo(1));
        Assert.That(ph[0].PositionHistoryRecord.RealizedPnL, Is.EqualTo(400));
        Assert.That(ph[0].PositionHistoryRecord.RealizedPnLInAccountCcy, Is.EqualTo(400));
        
        _accountEur.ProcessTrade(new("AS1", 100, null, _accountEur.AccountId, 10000, null, 1000, null, null, null, Side.Sell, 4, 1100, 44, _clock.GetCurrentInstant(),
            null, null, null, 840, 0.8m, 4400, null, null, false), _clock.GetCurrentInstant());

        ph = _projections.Projections
            .Where(p => p is PositionChangedEvt ph && ph.Type == PositionChangeType.Close && ph.AccountId == _accountEur.AccountId)
            .Select(p => (PositionChangedEvt)p)
            .ToList();
        Assert.That(ph.Count, Is.EqualTo(1));
        Assert.That(ph[0].PositionHistoryRecord.RealizedPnL, Is.EqualTo(400));
        Assert.That(ph[0].PositionHistoryRecord.RealizedPnLInAccountCcy, Is.EqualTo(320));
    }

    [Test, Order(3)]
    public void Test3RunEndOfDay()
    {
        _data.SetConversionPath(978, 840, new FxConversionStep() { ContractId = 10100, IsDirect = true });
        _data.SetConversionPath(840, 978, new FxConversionStep() { ContractId = 10100, IsDirect = false });
        
        _clock.Instant += Duration.FromMinutes(1);
        var eodPrices = new Dictionary<int, decimal>
        {
            { 10000, 1050 },
            { 10100, 1.25m },
        };
        _accountUsd.MarkToMarketEod(eodPrices, _clock.GetCurrentInstant(), _clock.GetCurrentInstant());
        _accountEur.MarkToMarketEod(eodPrices, _clock.GetCurrentInstant(), _clock.GetCurrentInstant());
        
        var ph = _projections.Projections
            .Where(p => p is PositionChangedEvt ph && ph.Type == PositionChangeType.MTM && ph.AccountId == _accountUsd.AccountId)
            .Select(p => (PositionChangedEvt)p)
            .ToList();
        Assert.That(ph.Count, Is.EqualTo(1));
        Assert.That(ph[0].PositionHistoryRecord.FloatingPnL, Is.EqualTo(300));
        
        Assert.That(_accountUsd.AccountStateReadonly.SharePrice, Is.EqualTo(1.02471111).Within(0.0001));
        
        ph = _projections.Projections
            .Where(p => p is PositionChangedEvt ph && ph.Type == PositionChangeType.MTM && ph.AccountId == _accountEur.AccountId)
            .Select(p => (PositionChangedEvt)p)
            .ToList();
        Assert.That(ph.Count, Is.EqualTo(1));
        Assert.That(ph[0].PositionHistoryRecord.FloatingPnL, Is.EqualTo(300));
        
        Assert.That(_accountUsd.AccountStateReadonly.SharePrice, Is.EqualTo(1.02471111).Within(0.0001));
        Assert.That(_accountEur.AccountStateReadonly.SharePrice, Is.EqualTo(1.02471111).Within(0.0001));
    }

    [Test, Order(4)]
    public void Test4CheckCurrencyRateChange()
    {
        _clock.Instant += Duration.FromMinutes(1);
        var eodPrices = new Dictionary<int, decimal>
        {
            { 10000, 1050 },
            { 10100, 1.5m },
        };
        _accountUsd.MarkToMarketEod(eodPrices, _clock.GetCurrentInstant(), _clock.GetCurrentInstant());
        _accountEur.MarkToMarketEod(eodPrices, _clock.GetCurrentInstant(), _clock.GetCurrentInstant());
        
        // EUR position revaluation
        Assert.That(_accountUsd.AccountStateReadonly.SharePrice, Is.EqualTo(1.02471111 + 0.11111).Within(0.0001)); // 1.13582111
        
        // Total USD position revaluation
        Assert.That(_accountEur.AccountStateReadonly.SharePrice, Is.EqualTo(1.02471111m - 1407.46m / 18000m).Within(0.0001)); // 0.94651889
    }

    [Test, Order(5)]
    public void Test5CheckPriceChange()
    {
        _clock.Instant += Duration.FromMinutes(1);
        var eodPrices = new Dictionary<int, decimal>
        {
            { 10000, 1100 },
            { 10100, 1.5m },
        };
        _accountUsd.MarkToMarketEod(eodPrices, _clock.GetCurrentInstant(), _clock.GetCurrentInstant());
        _accountEur.MarkToMarketEod(eodPrices, _clock.GetCurrentInstant(), _clock.GetCurrentInstant());
        
        Assert.That(_accountUsd.AccountStateReadonly.SharePrice, Is.EqualTo(1.13582111m + 50m * 6m / 22500m).Within(0.0001));
        Assert.That(_accountEur.AccountStateReadonly.SharePrice, Is.EqualTo(0.94651889m + 50m * 6m / 18000m / 1.5m).Within(0.0001));
    }
}

class StaticData : 
    IQueryHandler<GetContract, Contract>,
    IQueryHandler<GetCurrency, Currency>,
    IQueryHandler<GetConversionRate, decimal?>,
    IQueryHandler<GetConversionPath, IReadOnlyCollection<FxConversionStep>>,
    IQueryHandler<GetAsset, Asset?>
{
    public Dictionary<long, Contract> Contracts { get; } = new();
    public Dictionary<long, Currency> Currencies { get; } = new();
    public Dictionary<string, decimal?> FxRates { get; } = new();
    public Dictionary<string, FxConversionStep> ConversionPaths { get; } = new();
    public Dictionary<int, Asset> Assets { get; } = new();
    
    public Contract Handle(GetContract query) => Contracts[query.ContractId];
    public Currency Handle(GetCurrency query) => Currencies[query.CurrencyId];
    
    public void SetRate(long from, long to, decimal? rate) => FxRates[$"{from}/{to}"] = rate;
    public decimal? Handle(GetConversionRate query) => 
        FxRates.TryGetValue($"{query.FromCcy}/{query.ToCcy}", out var rate) ? rate : null;

    public void SetConversionPath(long from, long to, FxConversionStep fxConversionStep) => 
        ConversionPaths[$"{from}/{to}"] = fxConversionStep;
    
    public IReadOnlyCollection<FxConversionStep> Handle(GetConversionPath query) =>
        ConversionPaths.TryGetValue($"{query.FromCurrencyId}/{query.ToCurrencyId}", out var path) 
            ? new List<FxConversionStep> { path } 
            : new List<FxConversionStep>();

    public Asset? Handle(GetAsset query) => Assets[query.AssetId];
}