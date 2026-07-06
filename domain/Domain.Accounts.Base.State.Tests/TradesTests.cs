using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Accounts.Base.State.Tests;

public class TradesTests
{
#pragma warning disable NUnit1032
    private readonly ServiceProvider _sp;
#pragma warning restore NUnit1032
    
    private const int TestAccountId = 100;
    private readonly Currency TestCurrency = new() { CurrencyId = 840, Decimals = 2 };
    private const long TestContractId = 1000;
    
    private AccountBaseState _state;

    public TradesTests()
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

    [Test]
    [TestCase(10000, Side.Buy, 100, 10, 5, SecurityType.Stock, 1, 840, 1000, 100, -1005)]
    [TestCase(10000, Side.Sell, 100, 10, 5, SecurityType.Stock, 1, 840, 1000, -100, 995)]
    public void TestStockTrades(int contractId, Side side, decimal volume, decimal price, decimal commission, SecurityType securityType,
        decimal fxRate, int paymentCcyId, decimal calculatedCcyLastQty, decimal expectedPositionSignedQty, decimal expectedBalance)
    {
        _state.Apply(new TradeEvt(0, _state.AccountId,
            new Trade("AS1", 10000, null, _state.AccountId, contractId, null, 1000, null, null, PositionEffect.Unknown, side, volume, price, commission, 
                Instant.FromUtc(2026, 2, 9, 12, 0), null, null, null, paymentCcyId, fxRate, calculatedCcyLastQty,
                null, null, false),
            _state.GetNextVersion(),
            Instant.FromUtc(2026, 2, 9, 12, 0),
            5000,
            2,
            securityType,
            2
        ), true);
        
        var balances = _state.Balances;
        Assert.That(balances.Count, Is.EqualTo(1));
        Assert.That(balances[paymentCcyId], Is.EqualTo(expectedBalance));

        var positions = _state.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));
        var pos = positions.Single();
        Assert.That(pos.SignedVolume, Is.EqualTo(expectedPositionSignedQty));
        Assert.That(pos.OpenPrice, Is.EqualTo(price));
    }

    [Test]
    [TestCase(Side.Buy)]
    [TestCase(Side.Sell)]
    public void TestFxTrades(Side side)
    {
        _state.Apply(new TradeEvt(0, _state.AccountId,
            new Trade("AS1", 10000, null, _state.AccountId, 10000, null, 1000, null, null, PositionEffect.Unknown, 
                side, 10, 5000, 20, 
                Instant.FromUtc(2026, 2, 9, 12, 0), null, null, null, 
                840, 1, 5000 * 10,
                null, null, false),
            _state.GetNextVersion(),
            Instant.FromUtc(2026, 2, 9, 12, 0),
            978,
            2,
            SecurityType.FX,
            2
        ), true);
        
        var positions = _state.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(0));

        var balances = _state.Balances;
        Assert.That(balances.Count, Is.EqualTo(2));
        
        Assert.That(balances, Contains.Key(978));
        Assert.That(balances[978], Is.EqualTo(10 * side.GetSign()));
        
        Assert.That(balances, Contains.Key(840));
        Assert.That(balances[840], Is.EqualTo(5000m * 10m * side.Invert().GetSign() - 20));
    }

    [Test]
    [TestCase(Side.Buy, SecurityType.Futures)]
    [TestCase(Side.Buy, SecurityType.CFD)]
    [TestCase(Side.Sell, SecurityType.Futures)]
    [TestCase(Side.Sell, SecurityType.CFD)]
    public void TestSimpleFuturesTrades(Side side, SecurityType securityType)
    {
        _state.Apply(new TradeEvt(0, _state.AccountId,
            new Trade("AS1", 10000, null, _state.AccountId, 10000, null, 1000, null, null, PositionEffect.Unknown, 
                side, 10, 5000, 20, 
                Instant.FromUtc(2026, 2, 9, 12, 0), null, null, null, 
                840, 1, 5000 * 10,
                null, null, false),
            _state.GetNextVersion(),
            Instant.FromUtc(2026, 2, 9, 12, 0),
            5000,
            2,
            securityType,
            2
        ), true);
        
        var positions = _state.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));
        var pos = positions.Single();
        Assert.That(pos.SignedVolume, Is.EqualTo(10 * side.GetSign()));
        Assert.That(pos.ContractId, Is.EqualTo(10000));
        Assert.That(pos.OpenPrice, Is.EqualTo(5000));

        var balances = _state.Balances;
        Assert.That(balances.Count, Is.EqualTo(1));
        
        Assert.That(balances, Contains.Key(840));
        Assert.That(balances[840], Is.EqualTo(-20));
    }
    
    [Test]
    [TestCase(Side.Buy, SecurityType.Futures)]
    [TestCase(Side.Buy, SecurityType.CFD)]
    [TestCase(Side.Sell, SecurityType.Futures)]
    [TestCase(Side.Sell, SecurityType.CFD)]
    public void TestInverseFuturesTrades(Side side, SecurityType securityType)
    {
        _state.Apply(new TradeEvt(0, _state.AccountId,
            new Trade("AS1", 10000, null, _state.AccountId, 10000, null, 1000, null, null, PositionEffect.Unknown, 
                side, 10000, 50000, 20, 
                Instant.FromUtc(2026, 2, 9, 12, 0), null, null, null, 
                1000, 1, 50000m / 10000,
                null, null, false),
            _state.GetNextVersion(),
            Instant.FromUtc(2026, 2, 9, 12, 0),
            5000,
            8,
            securityType,
            8
        ), true);
        
        var positions = _state.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));
        var pos = positions.Single();
        Assert.That(pos.SignedVolume, Is.EqualTo(10000 * side.GetSign()));
        Assert.That(pos.ContractId, Is.EqualTo(10000));
        Assert.That(pos.OpenPrice, Is.EqualTo(50000));

        var balances = _state.Balances;
        Assert.That(balances.Count, Is.EqualTo(1));
        
        Assert.That(balances, Contains.Key(840));
        Assert.That(balances[840], Is.EqualTo(-20));
    }
}