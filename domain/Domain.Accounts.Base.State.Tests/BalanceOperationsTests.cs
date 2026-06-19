using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;

namespace Domain.Accounts.Base.State.Tests;

[TestOf(typeof(AccountBaseState))]
public class BalanceOperationsTests
{
#pragma warning disable NUnit1032
    private readonly ServiceProvider _sp;
#pragma warning restore NUnit1032
    
    private const int TestAccountId = 100;
    private readonly Currency TestCurrency = new() { CurrencyId = 840, Decimals = 2 };
    private const long TestContractId = 1000;
    
    private AccountBaseState _state;

    public BalanceOperationsTests()
    {
        _sp = new ServiceCollection()
            .AddLogging()
            .UseSingletonInMemoryBus()
            .BuildServiceProvider();
    }
    
    [SetUp]
    public void Setup()
    {
        _state = AccountBaseState.CreateNewState(new AccountRecordV6(
            "TEST",
            "Test Account",
            840,
            AccountType.VirtualAccount,
            PositionAccounting.Netted,
            null,
            true,
            true,
            null,
            TestAccountId
        ), _sp.GetRequiredService<IEventBus>(), _sp.GetRequiredService<ILoggerFactory>());
    }

    #region Balances
    
    [Test]
    public void CreateNewState_InitializesWithEmptyCollections()
    {
        Assert.That(_state.AccountId, Is.EqualTo(TestAccountId));
        Assert.That(_state.PositionAccounting, Is.EqualTo(PositionAccounting.Netted));
        Assert.That(_state.Balances, Is.Empty);
        Assert.That(_state.Orders, Is.Empty);
        Assert.That(_state.Positions, Is.Empty);
        Assert.That(_state.RealizedPnLSinceLastMtm, Is.EqualTo(0m));
        Assert.That(_state.Version, Is.EqualTo(0));
    }

    [Test]
    public void TestSetBalance()
    {
        var boEvt = new BalanceOperationProcessedEvt(1, TestAccountId, _state.GetNextVersion(),
            new BalanceOperation("TEST", 1, TestAccountId, Instant.FromUtc(2026, 2, 4, 13, 0), 
                100m, 840, 1, 1, 100m, null, null, false, false, true, true, true),
            Instant.FromUtc(2026, 2, 4, 13, 0),
            null
        );
        
        _state.Apply(boEvt, true);
        Assert.That(_state.Balances.Count, Is.EqualTo(1));
        Assert.That(_state.Balances[TestCurrency.CurrencyId], Is.EqualTo(100m));
    }

    [Test]
    public void TestUpdateBalance()
    {
        TestSetBalance();
        
        var boEvt = new BalanceOperationProcessedEvt(1, TestAccountId, _state.GetNextVersion(),
            new BalanceOperation("TEST",1, TestAccountId, Instant.FromUtc(2026, 2, 4, 13, 0), 
                50m, 840, 1, 1, 50m, null, null, false, false, true, true, true),
            Instant.FromUtc(2026, 2, 4, 13, 0),
            null
        );
        
        _state.Apply(boEvt, true);
        Assert.That(_state.Balances.Count, Is.EqualTo(1));
        Assert.That(_state.Balances[TestCurrency.CurrencyId], Is.EqualTo(150m));
    }

    [Test]
    public void TestRemoveBalance()
    {
        TestSetBalance();
        
        var boEvt = new BalanceOperationProcessedEvt(1, TestAccountId, _state.GetNextVersion(),
            new BalanceOperation("TEST",1, TestAccountId, Instant.FromUtc(2026, 2, 4, 13, 0), 
                -100m, 840, 1, 1, -100m, null, null, false, false, true, true, true),
            Instant.FromUtc(2026, 2, 4, 13, 0),
            null
        );
        
        _state.Apply(boEvt, true);
        Assert.That(_state.Balances.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestDuplicateBalanceEvent()
    {
        TestSetBalance();
        
        var boEvt = new BalanceOperationProcessedEvt(1, TestAccountId, _state.Version,
            new BalanceOperation("TEST", 1, TestAccountId, Instant.FromUtc(2026, 2, 4, 13, 0), 
                100m, 840, 1, 1, 100m, null, null, false, false, true, true, true),
            Instant.FromUtc(2026, 2, 4, 13, 0),
            null
        );
        
        _state.Apply(boEvt, true);
        Assert.That(_state.Balances.Count, Is.EqualTo(1));
        Assert.That(_state.Balances[TestCurrency.CurrencyId], Is.EqualTo(100m));
    }
    
    #endregion
    
    
    #region Orders

    public void TestSetOrder()
    {
        var evt = new ExecutionReportEvt(1, TestAccountId, _state.GetNextVersion(), AccountType.VirtualAccount,
            Order.CreateOrder(
                NewOrderSingle.MarketOrder("test", TestAccountId, 10000, string.Empty, PositionEffect.Unknown, 10,
                    Side.Buy),
                "AS1",
                1,
                1,
                Instant.FromUtc(2026, 2, 4, 13, 0),
                false),
            Instant.FromUtc(2026, 2, 4, 13, 0)
        );
        
        _state.Apply(evt, true);
        Assert.That(_state.Orders.Count, Is.EqualTo(1));
        Assert.That(_state.Version, Is.EqualTo(1));
    }
    
    #endregion
}