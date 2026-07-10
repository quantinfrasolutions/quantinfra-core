using Domain.Accounts.Base.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.AccountsService.Projections;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Tests.Mocks;
using GenericMockEventHandler = Domain.Accounts.Base.Tests.GenericMockEventHandler;
using MockIdsProvider = Domain.Accounts.Base.Tests.MockIdsProvider;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Domain.Accounts.Base.Tests;

[TestOf(typeof(AccountBase))]
public class AccountBaseStateTests
{
    private AccountBase _account;
    private AccountBaseState _state;
    private GenericMockEventHandler _events;
    private MockProjectionHandler _projections;
    private MockQueryHandler<GetConversionRate, decimal?> _fxRateQueryHandler;

    [SetUp]
    public void Setup()
    {
        _events = new GenericMockEventHandler();
        _projections = new MockProjectionHandler();

        var contractQueryHandler = new MockQueryHandler<GetContract, Contract>
        {
            Result = new(
                10000,
                "TEST",
                new ContractTemplate { SettlementCurrency = new Currency { CurrencyId = 840, } },
                null, null, null, null, null,
                "test-id",
                new Asset { AssetId = 100, Name = "STK" },
                null, new List<Stream>(),
                1
            )
        };

        var currencyQueryHandler = new MockQueryHandler<GetCurrency, Currency>()
        {
            Result = new Currency() { CurrencyId = 840, Decimals = 2 },
        };

        var assetQueryHandler = new MockQueryHandler<GetAsset, Asset?>()
        {
            Result = new() { AssetId = 840, },
        };

        _fxRateQueryHandler = new MockQueryHandler<GetConversionRate, decimal?>();

        var serviceProvider = new ServiceCollection()
            .UseSingletonInMemoryBus()
            .AddLogging(c =>
            {
                c.ClearProviders();
                c.AddFilter("*", LogLevel.Debug).AddConsole();
            })
            .AddSingleton<IEventHandler>(_events)
            .AddSingleton<IQueryHandler<GetContract, Contract>>(contractQueryHandler)
            .AddSingleton<IQueryHandler<GetCurrency, Currency>>(currencyQueryHandler)
            .AddSingleton<IQueryHandler<GetConversionRate, decimal?>>(_fxRateQueryHandler)
            .AddSingleton<IQueryHandler<GetAsset, Asset?>>(assetQueryHandler)
            .AddSingleton<IProjectionWriter>(_projections)
            
            .BuildServiceProvider();
        
        var accountRecord = new AccountRecordV6("AS","Test account", 840, AccountType.VirtualAccount,
            PositionAccounting.Netted, null, true, true, null, 100);

        _state = AccountBaseState.CreateNewState(accountRecord, 
            serviceProvider.GetRequiredService<IEventBus>(), serviceProvider.GetRequiredService<ILoggerFactory>());

        _account = new AccountBase(
            accountRecord,
            _state,
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

    [Test]
    public void TestSimpleBalanceOperation()
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        _account.ProcessBalanceOperation(new NewBalanceOperation()
        {
            AccountId = 100,
            AffectsBalance = true,
            AffectsInvestment = true,
            AffectsPnL = false,
            Amount = 100,
            AssetId = 840,
        }, now);
        
        Assert.That(_state.Balances.Count, Is.EqualTo(1));
        Assert.That(_state.Balances, Contains.Key(840));
        Assert.That(_state.Balances[840], Is.EqualTo(100));
        Assert.That(_state.ShareCount, Is.EqualTo(100));
        
        Assert.That(_events.Events.Count, Is.EqualTo(2)); // BalanceOperationProcessed, ShareCountUpdated
        Assert.That(_projections.Projections.Count, Is.EqualTo(2));
        
        var projection = _projections.Projections[0];
        Assert.That(projection, Is.TypeOf<BalanceHistoryProjectionEvt>());
        var bh = (BalanceHistoryProjectionEvt)projection;
        Assert.That(bh is { Change: 100, CurrencyId: 840, Balance: 100});
        Assert.That(bh.Timestamp, Is.EqualTo(now));
        
        projection = _projections.Projections[1];
        Assert.That(projection, Is.TypeOf<SharePriceHistoryProjectionEvt>());
        var sp = (SharePriceHistoryProjectionEvt)projection;
        Assert.That(sp.SharePrice is { ShareCount: 100, SharePrice: 1, Investment: 100, HWM: 1, Type: SharePriceHistoryChangeType.BalanceOperation  });
        Assert.That(sp.SharePrice.Dt, Is.EqualTo(now));

        projection = _projections.Projections[1];
    }

    [Test]
    public void TestBalanceOperationInAnotherCurrency()
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        
        _fxRateQueryHandler.Result = 1.2m;
        
        _account.ProcessBalanceOperation(new NewBalanceOperation()
        {
            AccountId = 100,
            AffectsBalance = true,
            AffectsInvestment = true,
            AffectsPnL = false,
            Amount = 100,
            AssetId = 978,
        }, now);
        
        Assert.That(_state.Balances.Count, Is.EqualTo(1));
        Assert.That(_state.Balances, Contains.Key(978));
        Assert.That(_state.Balances[978], Is.EqualTo(100));
        Assert.That(_state.ShareCount, Is.EqualTo(120));
        Assert.That(_state.Investment, Is.EqualTo(120));
        
        Assert.That(_events.Events.Count, Is.EqualTo(2)); // BalanceOperationProcessed, ShareCountUpdated
    }
}