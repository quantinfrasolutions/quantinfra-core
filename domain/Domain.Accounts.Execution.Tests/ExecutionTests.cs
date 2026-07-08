using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Account.Execution.State;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Accounts.Execution.EventHandlers.BrokerAccounts;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.ExternalAccounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Sdk.Trading.Positions;
using QuantInfra.Tests.Mocks;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Domain.Accounts.Execution.Tests;

[TestOf(typeof(StrategySubaccount))]
[TestOf(typeof(BrokerAccount))]
[TestOf(typeof(BrokerAccountState))]
[TestOf(typeof(SendActivatedOrderToBrokerAccount))]
public class ExecutionTests
{
    private BrokerAccountState _baState;
    private BrokerAccount _ba;

    private StrategySubaccount _ssa;
    private AccountBaseState _ssaState;
    
    private MockIdsProvider _idsProvider;
    private MockQueryHandler<GetCurrency, Currency?> _currencyQueryHandler;
    private MockDictionaryQueryHandler<GetContract, int, Contract?> _contractQueryHandler;
    private MockQueryHandler<GetBrokerAccountForSsa, int?> _getBrokerAccountForSsa;
    private MockQueryHandler<GetAccount, IBrokerAccount?> _getBrokerAccount;
    private MockDictionaryQueryHandler<GetAccount, int, IAccount?> _getAccount;
    private MockDictionaryQueryHandler<GetContractByExternalId, string, Contract?> _getContractByExternalId;
    private MockQueryHandler<GetSsaIdsForBrokerAccount, IReadOnlyCollection<int>> _getSsaIdsForBrokerAccount;
    private MockQueryHandler<GetBroker, Broker?> _getBroker;
    private GenericMockEventHandler _allEvents;
    private GenericMockEventHandler<TradeEvt> _internalTradesHistory;
    
    private readonly int _accountId = 1000001;
    private readonly int _brokerAccountId = 1000000;
    private MockClock _clock;
#pragma warning disable NUnit1032
    private ServiceProvider _serviceProvider;
#pragma warning restore NUnit1032

    private static Currency _usd = new()
    {
        Asset = new() { AssetId = 840, AssetType = AssetType.Currency, Name = "USD" },
        CurrencyId = 840,
        Decimals = 2,
    };
    
    private static Contract _contractWithNoExternalId = new Contract(10000, "Test",
        new(10000, "Test", SecurityType.Stock, new() { AssetId = 10000, }, 1, null, 10000, null, 1, 0.01m, null, 1, _usd,
        PnLCalculatorType.Default, null,
        null, null, new List<CommissionStructure>(), new List<TradingSession>(), new Exchange(),
        new Broker() { BrokerId = 100 },
        252, null),
        null, null, null, null, null, null, null, null, new List<Stream>(), 100);
    
    private static Contract _contractWithExternalId = new Contract(10000, "Test",
        new(10000, "Test", SecurityType.Stock, new() { AssetId = 10000, }, 1, null, 10000, null, 1, 0.01m, null, 1, _usd,
            PnLCalculatorType.Default, null,
            null, null, new List<CommissionStructure>(), new List<TradingSession>(), new Exchange(),
            new Broker() { BrokerId = 100 },
            252, null),
        null, null, null, null, null, "ext-10000", null, null, new List<Stream>(), 100);

    [SetUp]
    public void SetUp()
    {
        _idsProvider = new();
        _currencyQueryHandler = new();
        _contractQueryHandler = new()
        {
            KeySelector = query => query.ContractId,
        };
        _allEvents = new();
        _getBrokerAccountForSsa = new();
        _getBrokerAccount = new();
        _getAccount = new()
        {
            KeySelector = query => query.AccountId,
        };
        _getContractByExternalId = new()
        {
            KeySelector = query => query.ExternalId,
        };
        _getSsaIdsForBrokerAccount = new();
        _internalTradesHistory = new();
        _getBroker = new();
        _getBroker.Result = new() { BrokerId = 100, BrokerType = BrokerType.Ibkr };
        
        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(_idsProvider)
            .UseSingletonInMemoryBus()
            .AddSingleton<MockClock>()
            .AddSingleton<IClock>(sp => sp.GetRequiredService<MockClock>())
            
            .AddSingleton<IQueryHandler<GetCurrency, Currency?>>(_currencyQueryHandler)
            .AddSingleton<IQueryHandler<GetContract, Contract?>>(_contractQueryHandler)
            .AddSingleton<IQueryHandler<GetBrokerAccountForSsa, int?>>(_getBrokerAccountForSsa)
            .AddSingleton<IQueryHandler<GetAccount, IBrokerAccount?>>(_getBrokerAccount)
            .AddSingleton<IQueryHandler<GetAccount, IAccount?>>(_getAccount)
            .AddSingleton<IQueryHandler<GetContractByExternalId, Contract?>>(_getContractByExternalId)
            .AddSingleton<IEventHandler<TradeEvt>>(_internalTradesHistory)
            .AddSingleton<IQueryHandler<GetSsaIdsForBrokerAccount, IReadOnlyCollection<int>>>(_getSsaIdsForBrokerAccount)
            .AddSingleton<IQueryHandler<GetBroker, Broker?>>(_getBroker)
            
            .AddSingleton<IEventHandler>(_allEvents)
            
            .AddExecutionAccounts()
            
            .BuildServiceProvider();

        _currencyQueryHandler.Result = _usd;
        
        _clock = _serviceProvider.GetRequiredService<MockClock>();
        
        _baState = new BrokerAccountState("AS-test", _brokerAccountId, PositionAccounting.Netted, new Dictionary<int, decimal>(),
            new List<OrderStatus>(), new List<Position>(), 1, 0, 1, 0, 0, 0,
            Instant.MinValue, Instant.MinValue, new List<string>(), new List<ExecutionReport>(),
            Instant.MinValue, new List<string>(), new List<ExternalTradeRecord>(),
            new List<string>(), new Dictionary<string, Instant>(),
            true, false, false, _serviceProvider.GetRequiredService<IEventBus>(), _serviceProvider.GetRequiredService<ILoggerFactory>()
        );
        
        _ba = new BrokerAccount(
            new("AS-test", "Broker Account 1", 840, AccountType.BrokerAccount, PositionAccounting.Netted, 100, false, false,
                null, _brokerAccountId),
            _baState,
            _serviceProvider.GetRequiredService<MockIdsProvider>(),
            _serviceProvider.GetRequiredService<MockIdsProvider>(),
            _serviceProvider.GetRequiredService<MockIdsProvider>(),
            _serviceProvider.GetRequiredService<MockIdsProvider>(),
            _serviceProvider.GetRequiredService<MockIdsProvider>(),
            _serviceProvider.GetRequiredService<IEventBus>(),
            _serviceProvider.GetRequiredService<IQueryBus>(),
            _serviceProvider.GetRequiredService<ILoggerFactory>(),
            LogLevel.Debug
        );
        
        (_ssaState, _ssa) = CreateSsa(_accountId);
    }

    private (AccountBaseState, StrategySubaccount) CreateSsa(int accountId)
    {
        var state = new AccountBaseState("AS-test", accountId, PositionAccounting.Netted, new Dictionary<int, decimal>(),
            new List<OrderStatus>(), new List<Position>(), 1, 0, 1, 0, 0, 0,
            _serviceProvider.GetRequiredService<IEventBus>(), _serviceProvider.GetRequiredService<ILoggerFactory>());
        
        var ssa = new StrategySubaccount(
            new("AS-test", $"SSA {accountId}", 840, AccountType.StrategySubAccount, PositionAccounting.Netted,
                null, true, true, null, accountId),
            state,
            _serviceProvider.GetRequiredService<MockIdsProvider>(),
            _serviceProvider.GetRequiredService<MockIdsProvider>(),
            _serviceProvider.GetRequiredService<MockIdsProvider>(),
            _serviceProvider.GetRequiredService<MockIdsProvider>(),
            _serviceProvider.GetRequiredService<MockIdsProvider>(),
            _serviceProvider.GetRequiredService<IEventBus>(),
            _serviceProvider.GetRequiredService<IQueryBus>(),
            _serviceProvider.GetRequiredService<ILoggerFactory>(),
            LogLevel.Debug
        );
        
        return (state, ssa);
    }

    [Test]
    public void TestPlaceOrderOnSsaWithNoContractConfigured()
    {
        _clock.CurrentInstant = Instant.FromUtc(2026, 3, 12, 12, 0);
        
        var order = NewOrderSingle.MarketOrder("test-1", _ssa.AccountId, 10000, null, PositionEffect.Open, 10, Side.Buy);
        _ssa.PlaceOrder(order, _clock.GetCurrentInstant());
        
        Assert.That(_allEvents.Events.Count, Is.EqualTo(1));
        var erEvt = _allEvents.Events[0] as ExecutionReportEvt;
        Assert.That(erEvt, Is.Not.Null);
        Assert.That(erEvt.ExecutionReport.OrdStatus, Is.EqualTo(OrdStatus.Rejected));
        Assert.That(_ssaState.Orders.Count(), Is.EqualTo(0));
        Assert.That(_baState.Orders.Count(), Is.EqualTo(0));
    }

    [Test]
    public void TestPlaceOrderOnSsaWithNoBrokerIdConfigured()
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        
        var order = NewOrderSingle.MarketOrder("test-1", _ssa.AccountId, 10000, null, PositionEffect.Open, 10, Side.Buy);
        _ssa.PlaceOrder(order, _clock.GetCurrentInstant());
        
        Assert.That(_allEvents.Events.Count, Is.EqualTo(1));
        var erEvt = _allEvents.Events[0] as ExecutionReportEvt;
        Assert.That(erEvt, Is.Not.Null);
        Assert.That(erEvt.ExecutionReport.OrdStatus, Is.EqualTo(OrdStatus.Rejected));
        Assert.That(_ssaState.Orders.Count(), Is.EqualTo(0));
    }

    [Test]
    public void TestPlaceOrderOnSsaWithBrokerAccountCannotBeRetrieved()
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getBrokerAccountForSsa.Result = _brokerAccountId;
        
        var order = NewOrderSingle.MarketOrder("test-1", _ssa.AccountId, 10000, null, PositionEffect.Open, 10, Side.Buy);
        _ssa.PlaceOrder(order, _clock.GetCurrentInstant());
        
        Assert.That(_allEvents.Events.Count, Is.EqualTo(1));
        var erEvt = _allEvents.Events[0] as ExecutionReportEvt;
        Assert.That(erEvt, Is.Not.Null);
        Assert.That(erEvt.ExecutionReport.OrdStatus, Is.EqualTo(OrdStatus.Rejected));
        Assert.That(_ssaState.Orders.Count(), Is.EqualTo(0));
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void TestOrderGetsRejectedWhenExternalIdNotConfigured(bool useBrokerAccount)
    {
        _contractQueryHandler.Result.Add(_contractWithNoExternalId.ContractId, _contractWithNoExternalId);
        _getBrokerAccountForSsa.Result = _brokerAccountId;
        _getBrokerAccount.Result = _ba;
        _getAccount.Result.Add(_accountId, _ssa);
        
        var accountId = useBrokerAccount ? _brokerAccountId : _accountId;
        IAccount account = useBrokerAccount ? _ba : _ssa;
        
        var order = NewOrderSingle.MarketOrder("test-1", accountId, 10000, null, PositionEffect.Open, 10, Side.Buy);
        account.PlaceOrder(order, _clock.GetCurrentInstant());
        
        Assert.That(_ssaState.Orders.Count(), Is.EqualTo(0));
        Assert.That(_baState.Orders.Count(), Is.EqualTo(0));

        Assert.That(_allEvents.Events.Count, Is.EqualTo(2));
        var erEvt = _allEvents.Events[1] as ExecutionReportEvt;
        Assert.That(erEvt, Is.Not.Null);
        Assert.That(erEvt.ExecutionReport.OrdStatus, Is.EqualTo(OrdStatus.Rejected));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void TestPlaceOrderWithEverythingConfigured(bool useBrokerAccount)
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getBrokerAccountForSsa.Result = _brokerAccountId;
        _getBrokerAccount.Result = _ba;
        _getAccount.Result.Add(_accountId, _ssa);
        
        var accountId = useBrokerAccount ? _brokerAccountId : _accountId;
        IAccount account = useBrokerAccount ? _ba : _ssa;
        
        var order = NewOrderSingle.MarketOrder("test-1", accountId, 10000, null, PositionEffect.Open, 10, Side.Buy);
        account.PlaceOrder(order, _clock.GetCurrentInstant());

        Assert.That(_allEvents.Events.Count, Is.EqualTo(2));
        var erEvt = _allEvents.Events[0] as ExecutionReportEvt;
        Assert.That(erEvt, Is.Not.Null);
        var er = erEvt.ExecutionReport;
        Assert.That(er.OrdStatus, Is.EqualTo(OrdStatus.PendingNew));
        Assert.That(er.AccountId, Is.EqualTo(accountId));
        Assert.That(er.BrokerAccountId, Is.EqualTo(_ba.AccountId));

        if (useBrokerAccount)
        {
            Assert.That(_ssaState.Orders.Count(), Is.EqualTo(0));
        }
        else
        {
            Assert.That(_ssaState.Orders.Count(), Is.EqualTo(1));
            Assert.That(_ssaState.Orders.Single().BrokerAccountId, Is.EqualTo(_ba.AccountId));
        }
        
        Assert.That(_baState.Orders.Count(), Is.EqualTo(1));
        Assert.That(_baState.Orders.Single().BrokerAccountId, Is.EqualTo(_ba.AccountId));
        Assert.That(_baState.Orders.Single().AccountId, Is.EqualTo(accountId));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void TestSuspendedOrderActivation(bool useBrokerAccount)
    {
        _clock.CurrentInstant = Instant.FromUtc(2026, 3, 12, 12, 0);
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getBrokerAccountForSsa.Result = _brokerAccountId;
        _getBrokerAccount.Result = _ba;
        _getAccount.Result.Add(_accountId, _ssa);
        
        var accountId = useBrokerAccount ? _brokerAccountId : _accountId;
        IAccount account = useBrokerAccount ? _ba : _ssa;
        
        var order = NewOrderSingle.MarketOrder("test-1", accountId, 10000, null, PositionEffect.Open, 10, Side.Buy,
            isSuspended: true, activationDt: Instant.FromUtc(2026, 3, 12, 13, 0));
        account.PlaceOrder(order, _clock.GetCurrentInstant());

        if (useBrokerAccount)
        {
            Assert.That(_ssaState.Orders.Count(), Is.EqualTo(0));
            Assert.That(_baState.Orders.Count(), Is.EqualTo(1));
            Assert.That(_baState.Orders.Single().IsSuspended, Is.EqualTo(true));
            
        }
        else
        {
            Assert.That(_ssaState.Orders.Count(), Is.EqualTo(1));
            Assert.That(_baState.Orders.Count(), Is.EqualTo(0));            
        }
        
        Assert.That(_allEvents.Events.Count, Is.EqualTo(1));
        var erEvt = _allEvents.Events.Single() as ExecutionReportEvt;
        Assert.That(erEvt, Is.Not.Null);
        _allEvents.Events.Clear();
        
        account.OnHeartbeat(Instant.FromUtc(2026, 3, 12, 13, 0));
        if (useBrokerAccount)
        {
            Assert.That(_ssaState.Orders.Count(), Is.EqualTo(0));
            Assert.That(_baState.Orders.Count(), Is.EqualTo(1));
            Assert.That(_baState.Orders.Single().IsSuspended, Is.False);
        }
        else
        {
            Assert.That(_ssaState.Orders.Count(), Is.EqualTo(1));
            Assert.That(_ssaState.Orders.Single().IsSuspended, Is.EqualTo(false));
            Assert.That(_baState.Orders.Count(), Is.EqualTo(1));            
        }
        
        Assert.That(_allEvents.Events.Count, Is.EqualTo(2));
        Assert.That(_allEvents.Events.Last(), Is.TypeOf<NewOrderSingleExternalCreatedEvt>());
    }
    
    [Test]
    public void TestOrdersFromDifferentAccountsWithSameClOrdIdAccepted()
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getBrokerAccountForSsa.Result = _brokerAccountId;
        _getBrokerAccount.Result = _ba;

        var ssa1 = CreateSsa(1000005).Item2;
        var ssa2 = CreateSsa(1000006).Item2;
        _getAccount.Result.Add(1000005, ssa1);
        _getAccount.Result.Add(1000006, ssa2);

        var order1 = NewOrderSingle.MarketOrder("test-1", 1000005, 10000, null, PositionEffect.Open, 10, Side.Buy);
        var order2 = NewOrderSingle.MarketOrder("test-1", 1000006, 10000, null, PositionEffect.Open, 10, Side.Buy);
        var order3 = NewOrderSingle.MarketOrder("test-1", _brokerAccountId, 10000, null, PositionEffect.Open, 10, Side.Buy);
        
        ssa1.PlaceOrder(order1, _clock.GetCurrentInstant());
        ssa2.PlaceOrder(order2, _clock.GetCurrentInstant());
        _ba.PlaceOrder(order3, _clock.GetCurrentInstant());
        
        Assert.That(_baState.Orders.Count, Is.EqualTo(3));
    }

    [Test]
    public void TestOrderGetsAccepted()
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getBrokerAccountForSsa.Result = _brokerAccountId;
        _getBrokerAccount.Result = _ba;
        _getAccount.Result.Add(_accountId, _ssa);
        
        var order = NewOrderSingle.MarketOrder("test-1", _accountId, 10000, null, PositionEffect.Open, 10, Side.Buy);
        _ssa.PlaceOrder(order, _clock.GetCurrentInstant());
        
        Assert.That(_baState.Orders.Count, Is.EqualTo(1));
        Assert.That(_ssaState.Orders.Count, Is.EqualTo(1));
        var orderId = _baState.Orders.Single().OrderId;
        
        var nosEvt = _allEvents.Events.Last() as NewOrderSingleExternalCreatedEvt;
        Assert.That(nosEvt, Is.Not.Null);
        
        _ba.OnExternalExecutionReport(nosEvt.Order.ConfirmSentForExecution("ext-1", _clock.GetCurrentInstant()), _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(1));
        Assert.That(_baState.Orders.Single().ExternalId, Is.EqualTo("ext-1"));
        Assert.That(_baState.Orders.Single().OrdStatus, Is.EqualTo(OrdStatus.PendingNew));
        
        Assert.That(_ssaState.Orders.Count, Is.EqualTo(1));
        Assert.That(_ssaState.Orders.Single().ExternalId, Is.EqualTo("ext-1"));
        Assert.That(_ssaState.Orders.Single().OrdStatus, Is.EqualTo(OrdStatus.PendingNew));
    }
    
    [Test]
    [TestCase(true, true)]
    [TestCase(false, false)]
    // [TestCase(false, true)] no longer can cancel order from another account
    public void TestOrderGetsCanceled(bool useBrokerAccount, bool cancelFromBrokerAccount)
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getBrokerAccountForSsa.Result = _brokerAccountId;
        _getBrokerAccount.Result = _ba;
        _getAccount.Result.Add(_accountId, _ssa);
        
        var accountId = useBrokerAccount ? _brokerAccountId : _accountId;
        IAccount account = useBrokerAccount ? _ba : _ssa;
        
        var order = NewOrderSingle.MarketOrder("test-1", accountId, 10000, null, PositionEffect.Open, 10, Side.Buy);
        account.PlaceOrder(order, _clock.GetCurrentInstant());
        
        Assert.That(_baState.Orders.Count, Is.EqualTo(1));
        Assert.That(_ssaState.Orders.Count, Is.EqualTo(useBrokerAccount ? 0 : 1));
        var orderId = _baState.Orders.Single().OrderId;
        
        var nosEvt = _allEvents.Events.Last() as NewOrderSingleExternalCreatedEvt;
        Assert.That(nosEvt, Is.Not.Null);
        
        _ba.OnExternalExecutionReport(nosEvt.Order.ConfirmSentForExecution("ext-1", _clock.GetCurrentInstant()), _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(1));
        Assert.That(_baState.Orders.Single().ExternalId, Is.EqualTo("ext-1"));
        
        account = cancelFromBrokerAccount ? _ba : _ssa;
        account.CancelOrder(new OrderCancelRequest() { AccountId = accountId, OrderId = orderId }, _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(1));
        Assert.That(_baState.Orders.Single().OrdStatus, Is.EqualTo(OrdStatus.PendingCancel));

        if (useBrokerAccount)
        {
            Assert.That(_ssaState.Orders.Count, Is.EqualTo(0));
        }
        else
        {
            Assert.That(_ssaState.Orders.Count, Is.EqualTo(1));
            Assert.That(_ssaState.Orders.Single().OrdStatus, Is.EqualTo(OrdStatus.PendingCancel));
        }
        
        _ba.OnExternalExecutionReport(new(null, nosEvt.Order.OrderId, null, _ba.AccountId, null, OrdStatus.Canceled, null,
            null, null, null, null, 0, 0, null, null, null, null, ExecType.Canceled, null, null, null, _clock.GetCurrentInstant(), null), 
            _clock.GetCurrentInstant()
        );
        
        Assert.That(_ssaState.Orders.Count, Is.EqualTo(0));
        Assert.That(_baState.Orders.Count, Is.EqualTo(0));
    }
    
    
    [Test]
    public void TextExternalERReceivedForUnknownOrder()
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getContractByExternalId.Result.Add(_contractWithExternalId.ExternalContractId, _contractWithExternalId);
    
        var externalEr = new ExternalExecutionReport(null, null, "ext-order-1", _brokerAccountId, 
            "ext-10000", OrdStatus.PartiallyFilled, OrdType.Market, Side.Sell, 
            7, 10000, 18, 10, 8, 100, null, TimeInForce.GoodTillCancelled, null, 
            ExecType.Fill, null, null, null, _clock.GetCurrentInstant(), null);
        
        _ba.OnExternalExecutionReport(externalEr, _clock.GetCurrentInstant());
        
        Assert.That(_baState.Orders.Count, Is.EqualTo(1));
        var order = _baState.Orders.Single();
        Assert.That(order.OrdStatus, Is.EqualTo(OrdStatus.PartiallyFilled));
        Assert.That(order.ExternalId, Is.EqualTo("ext-order-1"));
    }
    
    [Test]
    public void TestExternalERReceivedForUnknownContract()
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
    
        var externalEr = new ExternalExecutionReport(null, null, "ext-order-1", _brokerAccountId, 
            "ext-10000", OrdStatus.PartiallyFilled, OrdType.Market, Side.Sell, 
            7, 10000, 18, 10, 8, 100, null, TimeInForce.GoodTillCancelled, null, 
            ExecType.Fill, null, null, null, _clock.GetCurrentInstant(), null);
        
        _ba.OnExternalExecutionReport(externalEr, _clock.GetCurrentInstant());
        
        Assert.That(_baState.Orders.Count, Is.EqualTo(0));
    }
    
    [Test]
    public void TextExternalERReceivedForUnknownOrderInTerminalStatus()
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getContractByExternalId.Result.Add(_contractWithExternalId.ExternalContractId, _contractWithExternalId);
    
        var externalEr = new ExternalExecutionReport(null, null, "ext-order-1", _brokerAccountId, 
            "ext-10000", OrdStatus.Filled, OrdType.Market, Side.Sell, 
            7, 10000, 7, 7, 0, 100, null, TimeInForce.GoodTillCancelled, null, 
            ExecType.Fill, null, null, null, _clock.GetCurrentInstant(), null);
        
        _ba.OnExternalExecutionReport(externalEr, _clock.GetCurrentInstant());
        
        Assert.That(_baState.Orders.Count, Is.EqualTo(0));
    }
    
    [Test]
    public void TestReceiveExternalTradeWithNoMatchingOrder()
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getContractByExternalId.Result.Add(_contractWithExternalId.ExternalContractId, _contractWithExternalId);
        _getSsaIdsForBrokerAccount.Result = [];
        
        _clock.CurrentInstant = Instant.FromUtc(2026, 3, 12, 12, 0);
        var tradeDt = _clock.GetCurrentInstant();
        var trade = new ExternalTradeRecord("test-trade-1", "", "ext-10000", _brokerAccountId, Side.Buy, 10, 100, 12, "USD", tradeDt, 1000);
        
        _ba.OnExternalTrade(trade, _clock.GetCurrentInstant());
    
        Assert.That(_internalTradesHistory.Events.Count, Is.EqualTo(1));
        var t = _internalTradesHistory.Events.Single().Trade;
        Assert.That(t, Is.EqualTo(new Trade
        {
            TradeId = t.TradeId, 
            AccountId = _brokerAccountId, 
            ContractId = 10000, 
            OrderId = null, 
            Side = Side.Buy, 
            Volume = 10, 
            Price = 100, 
            CalculatedCcyLastQty = 1000,
            Commission = 12, 
            Dt = tradeDt,
            ExecutionRequestId = null, 
            ExternalTradeId = "test-trade-1",
            PaymentCurrencyId = 840,
            FxRate = 1,
        }));
        
        var positions = _baState.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));
        Assert.That(positions[0] is { Side: Side.Buy, Volume: 10, OpenPrice: 100, Commission: 12 });
       
        _clock.CurrentInstant += Duration.FromSeconds(10);
        
        // Try to process the same trade once again, ensure it's ignored
        _ba.OnExternalTrade(trade, _clock.GetCurrentInstant());
        Assert.That(_internalTradesHistory.Events.Count, Is.EqualTo(1));
        positions = _baState.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));
        Assert.That(positions[0] is { Side: Side.Buy, Volume: 10, OpenPrice: 100, Commission: 12 });
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void TestReceiveExternalTrade(bool useBrokerAccount)
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getContractByExternalId.Result.Add(_contractWithExternalId.ExternalContractId, _contractWithExternalId);
        _getBrokerAccountForSsa.Result = _brokerAccountId;
        _getBrokerAccount.Result = _ba;
        _getAccount.Result.Add(_accountId, _ssa);
        _getSsaIdsForBrokerAccount.Result = [_accountId];
        
        var accountId = useBrokerAccount ? _brokerAccountId : _accountId;
        IAccount account = useBrokerAccount ? _ba : _ssa;
        
        var order = NewOrderSingle.MarketOrder("test-1", accountId, 10000, null, PositionEffect.Open, 10, Side.Buy);
        account.PlaceOrder(order, _clock.GetCurrentInstant());
        var orderId = _baState.Orders.Single().OrderId;
        
        _clock.CurrentInstant = Instant.FromUtc(2026, 3, 12, 12, 0);
        
        account.PlaceOrder(order, _clock.GetCurrentInstant());
        var nos = _allEvents.Events.OfType<NewOrderSingleExternalCreatedEvt>().Last().Order;
        
        _clock.CurrentInstant += Duration.FromSeconds(1);
        var accept = nos.ConfirmSentForExecution("ext-1", _clock.GetCurrentInstant());
        
        _ba.OnExternalExecutionReport(accept, _clock.GetCurrentInstant());
        
        _clock.CurrentInstant += Duration.FromSeconds(1);
        var fill = new ExternalExecutionReport(nos, "ext-1", OrdStatus.PartiallyFilled, ExecType.Fill, null,
            3, 100, 3, 7, _clock.GetCurrentInstant(), 300);
        _ba.OnExternalExecutionReport(fill, _clock.GetCurrentInstant());
        
        Assert.That(_baState.Orders.Count, Is.EqualTo(1));
        Assert.That(_baState.Orders.Single().CumQty, Is.EqualTo(3));
        Assert.That(_baState.Orders.Single().LeavesQty, Is.EqualTo(7));
        Assert.That(_baState.PendingFills.Count, Is.EqualTo(1));
        var fillExecId = _baState.PendingFills.Single().Key;

        if (useBrokerAccount)
        {
            Assert.That(_ssaState.Orders.Count, Is.EqualTo(0));
        }
        else
        {
            Assert.That(_ssaState.Orders.Count, Is.EqualTo(1));
            Assert.That(_ssaState.Orders.Single().CumQty, Is.EqualTo(3));
            Assert.That(_ssaState.Orders.Single().LeavesQty, Is.EqualTo(7));
        }

        var trade = new ExternalTradeRecord("test-trade-1", fill.ExternalId, "ext-10000", _brokerAccountId, Side.Buy, 3, 100, 12, "USD", _clock.GetCurrentInstant(), 300);
        
        _ba.OnExternalTrade(trade, _clock.GetCurrentInstant());
    
        Assert.That(_internalTradesHistory.Events.Count, Is.EqualTo(2)); // Trade and allocation
        var t = _internalTradesHistory.Events.First().Trade;
        Assert.That(t, Is.EqualTo(new Trade
        {
            AccountServiceName = "AS-test",
            TradeId = t.TradeId, 
            AccountId = _brokerAccountId, 
            ContractId = 10000, 
            ClOrdId = "test-1",
            OrderId = orderId, 
            ExecId = fillExecId,
            Side = Side.Buy, 
            PositionEffect = PositionEffect.Open,
            Volume = 3, 
            Price = 100, 
            CalculatedCcyLastQty = 300,
            Commission = 12, 
            Dt = _clock.GetCurrentInstant() ,
            ExecutionRequestId = null, 
            ExternalTradeId = "test-trade-1",
            PaymentCurrencyId = 840,
            FxRate = 1,
        }));
        
        var positions = _baState.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));
        Assert.That(positions[0] is { Side: Side.Buy, Volume: 3, OpenPrice: 100, Commission: 12 });
        Assert.That(_baState.PendingFills.Count, Is.EqualTo(0));
        
        positions = _ssaState.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));
        Assert.That(positions[0] is { Side: Side.Buy, Volume: 3, OpenPrice: 100, Commission: 12 });
        
        _clock.CurrentInstant += Duration.FromSeconds(10);
        // Try to process the same trade once again, ensure it's ignored
        _ba.OnExternalTrade(trade, _clock.GetCurrentInstant());
        Assert.That(_internalTradesHistory.Events.Count, Is.EqualTo(2));
        positions = _baState.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));
        Assert.That(positions[0] is { Side: Side.Buy, Volume: 3, OpenPrice: 100, Commission: 12 });
    }
    
    [Test]
    public void TestReceiveExternalPositionReportWithMatchingPositions()
    {
        // _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getContractByExternalId.Result.Add(_contractWithExternalId.ExternalContractId, _contractWithExternalId);
        _clock.CurrentInstant = Instant.FromUtc(2026, 3, 12, 12, 0);
        
        _baState.Apply(new TradeEvt(
            _idsProvider.GetNextEventId(),
            _brokerAccountId,
            new Trade("AS-test", _idsProvider.GetNextTradeId(), null, _brokerAccountId, 10000,
                null, null, null, null, null, Side.Sell, 7, 10, 0, 
                _clock.GetCurrentInstant(), null, null, null, 
                840, 1, 70, null, null, false
            ),
            _baState.GetNextVersion(),
            _clock.GetCurrentInstant(),
            10000,
            2,
            SecurityType.Stock,
            2,
            PnLCalculatorType.Default, 0.01m, 0.01m, 1m
        ), true);
        _internalTradesHistory.Events.Clear();
        
        var posRpt = new ExternalPositionReport
        {
            AccountId = _brokerAccountId,
            BrokerId = 100,
            ExternalContractId = "ext-10000",
            OpenDt = _clock.GetCurrentInstant(),
            OpenPrice = 10,
            SignedVolume = -7
        };
        _ba.OnExternalPositionReport(posRpt, _clock.GetCurrentInstant());
        Assert.That(_internalTradesHistory.Events.Count, Is.EqualTo(0));
    }
    
    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void TestReceiveExternalPositionReportWithLargerVolume(int numberOfSsas)
    {
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getContractByExternalId.Result.Add(_contractWithExternalId.ExternalContractId, _contractWithExternalId);
        _clock.CurrentInstant = Instant.FromUtc(2026, 3, 12, 12, 0);

        _getSsaIdsForBrokerAccount.Result = Enumerable.Range(0, numberOfSsas).Select(i => 1000010 + i).ToList();
        var accounts = _getSsaIdsForBrokerAccount.Result.Select(CreateSsa).ToList();
        foreach (var account in accounts)
        {
            _getAccount.Result[account.Item2.AccountId] = account.Item2;
        }
        
        _baState.Apply(new TradeEvt(
            _idsProvider.GetNextEventId(),
            _brokerAccountId,
            new Trade("AS-test", _idsProvider.GetNextTradeId(), null, _brokerAccountId, 10000,
                null, null, null, null, null, Side.Sell, 7, 10, 0, 
                _clock.GetCurrentInstant(), null, null, null, 
                840, 1, 70, null, null, false
            ),
            _baState.GetNextVersion(),
            _clock.GetCurrentInstant(),
            10000,
            2,
            SecurityType.Stock,
            2,
            PnLCalculatorType.Default, 0.01m, 0.01m, 1m
        ), true);
        _internalTradesHistory.Events.Clear();
        
        var posRpt = new ExternalPositionReport
        {
            AccountId = _brokerAccountId,
            BrokerId = 1,
            ExternalContractId = "ext-10000",
            OpenDt = _clock.GetCurrentInstant(),
            OpenPrice = 11,
            SignedVolume = -8
        };
        _ba.OnExternalPositionReport(posRpt, _clock.GetCurrentInstant());
        
        var positions = _baState.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));
        var pos = positions.Single();
        Assert.That(pos.SignedVolume, Is.EqualTo(-8));
        Assert.That(pos.OpenPrice, Is.EqualTo(11));
        Assert.That(pos.ContractId, Is.EqualTo(10000));
        
        Assert.That(_internalTradesHistory.Events.Count, Is.EqualTo(2 + (numberOfSsas == 1 ? 2 : 0))); // 2 corrections, 2 possible allocations
    }
    
    
    [Test]
    public void TestReceiveOrdersSnapshot()
    {
        _clock.CurrentInstant = Instant.FromUtc(2026, 3, 12, 12, 0);

        var secondAccountId = 1000010;
        var (ssaState2, ssa2) = CreateSsa(secondAccountId);
        
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _getContractByExternalId.Result.Add(_contractWithExternalId.ExternalContractId, _contractWithExternalId);
        _getBrokerAccountForSsa.Result = _brokerAccountId;
        _getBrokerAccount.Result = _ba;
        _getSsaIdsForBrokerAccount.Result = [_accountId, secondAccountId];
        _getAccount.Result.Add(_accountId, _ssa);
        _getAccount.Result.Add(secondAccountId, ssa2);
        
        _idsProvider.OrderId = 1;
        
        // External account, unchanged, matched by external id
        var o1 = NewOrderSingle.MarketOrder("m-1", _accountId, 10000, "0", PositionEffect.Open, 10, Side.Buy);
        _ssa.PlaceOrder(o1, _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(1));
        var evt = _allEvents.Events.OfType<NewOrderSingleExternalCreatedEvt>().Last();
        var er = evt.Order.ConfirmSentForExecution("ext-m-1-1", _clock.GetCurrentInstant());
        _ba.OnExternalExecutionReport(er, _clock.GetCurrentInstant());
        
        
        // External account, updated
        var o2 = NewOrderSingle.LimitOrder("L-2", _accountId, 10000, "0", PositionEffect.Open, 10, Side.Buy, 9200);
        _ssa.PlaceOrder(o2, _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(2));
        
        // External account, missing
        var o3 = NewOrderSingle.LimitOrder("S-3", _accountId, 10000, "0", PositionEffect.Open, 10, Side.Sell, 10100);
        _ssa.PlaceOrder(o3, _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(3));
        
        // Broker account, unchanged, matched by external id
        var o4 = NewOrderSingle.MarketOrder("m-4", _brokerAccountId, 10000, "0", PositionEffect.Open, 10, Side.Buy);
        _ba.PlaceOrder(o4, _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(4));
        evt = _allEvents.Events.OfType<NewOrderSingleExternalCreatedEvt>().Last();
        er = evt.Order.ConfirmSentForExecution("ext-m-1-2", _clock.GetCurrentInstant());
        _ba.OnExternalExecutionReport(er, _clock.GetCurrentInstant());
        
        // Broker account, updated
        var o5 = NewOrderSingle.LimitOrder("L-5", _brokerAccountId, 10000, "0", PositionEffect.Open, 10, Side.Buy, 9200);
        _ba.PlaceOrder(o5, _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(5));
        
        // Broker account, missing
        var o6 = NewOrderSingle.StopMarketOrder("S-6", _brokerAccountId, 10000, "0", PositionEffect.Open, 10, Side.Sell, 10100);
        _ba.PlaceOrder(o6, _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(6));
        
        // Second account, unchanged, matched by external id
        
        var o7 = NewOrderSingle.MarketOrder("m-7", secondAccountId, 10000, "0", PositionEffect.Open, 10, Side.Buy);
        ssa2.PlaceOrder(o7, _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(7));
        evt = _allEvents.Events.OfType<NewOrderSingleExternalCreatedEvt>().Last();
        er = evt.Order.ConfirmSentForExecution("ext-m-1-3", _clock.GetCurrentInstant());
        _ba.OnExternalExecutionReport(er, _clock.GetCurrentInstant());
        
        // Broker account, updated
        var o8 = NewOrderSingle.LimitOrder("L-8", _brokerAccountId, 10000, "0", PositionEffect.Open, 10, Side.Buy, 9200);
        _ba.PlaceOrder(o8, _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(8));
        
        // Broker account, missing
        var o9 = NewOrderSingle.StopMarketOrder("S-9", _brokerAccountId, 10000, "0", PositionEffect.Open, 10, Side.Sell, 10100);
        _ba.PlaceOrder(o9, _clock.GetCurrentInstant());
        Assert.That(_baState.Orders.Count, Is.EqualTo(9));
        
        Assert.That(_ssaState.Orders.Count, Is.EqualTo(3));
        Assert.That(ssaState2.Orders.Count, Is.EqualTo(1));
    
        var snapshot = new ExternalAccountOrdersSnapshot()
        {
            AccountId = _brokerAccountId,
            Orders =
            [
                // External account
                
                // Unchanged order, matched by external id
                new ExternalExecutionReport("m-1", null, "ext-m-1-1", _brokerAccountId, "ext-10000",
                    OrdStatus.New, OrdType.Market, Side.Buy, 0, null,
                    10, 0, 10, null, null, TimeInForce.GoodTillCancelled, null, ExecType.OrderStatus, null, null, null,
                    _clock.GetCurrentInstant(), null),
                
                // Order with changed attributes
                new ExternalExecutionReport("L-2", 2, "ext-L-1-1", _brokerAccountId, "ext-10000",
                    OrdStatus.New, OrdType.Limit, Side.Sell, 0, null,
                    5, 0, 5, 9400, null, TimeInForce.GoodTillCancelled, null, ExecType.OrderStatus, null, null, null,
                    _clock.GetCurrentInstant(), null),
                
                // o3 is missing
                
                // Broker account
                
                // Unchanged order, matched by external id
                new ExternalExecutionReport("m-4", null, "ext-m-1-2", _brokerAccountId, "ext-10000",
                    OrdStatus.New, OrdType.Market, Side.Buy, 0, null,
                    10, 0, 10, null, null, TimeInForce.GoodTillCancelled, null, ExecType.OrderStatus, null, null, null,
                    _clock.GetCurrentInstant(), null),
                
                // Order with changed attributes
                new ExternalExecutionReport("L-5", 5, "ext-L-1-2", _brokerAccountId, "ext-10000",
                    OrdStatus.New, OrdType.Limit, Side.Sell, 0, null,
                    5, 0, 5, 9400, null, TimeInForce.GoodTillCancelled, null, ExecType.OrderStatus, null, null, null,
                    _clock.GetCurrentInstant(), null),
                
                // o3 is missing
                
                // Second account
                
                // Unchanged order, matched by external id
                new ExternalExecutionReport("m-6", null, "ext-m-1-3", _brokerAccountId, "ext-10000",
                    OrdStatus.New, OrdType.Market, Side.Buy, 0, null,
                    10, 0, 10, null, null, TimeInForce.GoodTillCancelled, null, ExecType.OrderStatus, null, null, null,
                    _clock.GetCurrentInstant(), null),
                
                // Order with changed attributes
                new ExternalExecutionReport("L-7", 8, "ext-L-1-3", _brokerAccountId, "ext-10000",
                    OrdStatus.New, OrdType.Limit, Side.Sell, 0, null,
                    5, 0, 5, 9400, null, TimeInForce.GoodTillCancelled, null, ExecType.OrderStatus, null, null, null,
                    _clock.GetCurrentInstant(), null),
                
                // o3 is missing
                
                // Unknown order
                new ExternalExecutionReport(null, null, "ext-new-order", _brokerAccountId, "ext-10000",
                    OrdStatus.New, OrdType.StopLimit, Side.Buy, 0, null,
                    12, 0, 12, 10300, 10400, TimeInForce.GoodTillCancelled, null, ExecType.OrderStatus, null, null, null,
                    _clock.GetCurrentInstant(), null)
            ],
            UpdateTs = _clock.GetCurrentInstant(),
        };
        
        _ba.OnExternalAccountOrdersSnapshot(snapshot, _clock.GetCurrentInstant());
        var orders = _baState.Orders.ToList();
        Assert.That(orders.Count, Is.EqualTo(7));
        Assert.That(_ssaState.Orders.Count, Is.EqualTo(2));
        Assert.That(ssaState2.Orders.Count, Is.EqualTo(1));
        Assert.That(orders.SingleOrDefault(o => o.OrderId == 3), Is.Null);
        
        var order = orders.SingleOrDefault(o => o.OrderId == 1);
        Assert.That(order, Is.Not.Null);
        // Assert.That(order, Is.EqualTo(new Order(o1) { ExternalId = "ext-m-1-1" }));
        
        order = orders.SingleOrDefault(o => o.OrderId == 2);
        Assert.That(order, Is.Not.Null);
        // Assert.That(order as Order, Is.EqualTo(new Order(o2)
        // {
        //     ExternalId = "ext-L-1-1", 
        //     Side = Side.Sell,
        //     OrderQty = 5,
        //     Price = 9400,
        // }));
        
        order = orders.SingleOrDefault(o => o.OrderId > 9);
        Assert.That(order, Is.Not.Null);
        // Assert.That(order as Order, Is.EqualTo(
        //     new Order(Order.StopLimitOrder(null, _brokerAccountId, 10000, "0", PositionEffect.Unknown,
        //     12, Side.Buy, 10300, 10400))
        //     {
        //         OrderId = order!.OrderId, // ignore
        //         ClOrdId = order.ClOrdId, // auto-assigned
        //         ExternalId = order.ExternalId, // ignore
        //         StrategyPositionId = "0", // default
        //     }
        // ));
                
        // var updates = _orderUpdates.Events
        //     .GroupBy(e => e.AccountId)
        //     .ToDictionary(gr => gr.Key, gr => gr.SelectMany(e => e.ChangedOrders).ToList());
        // Assert.That(updates.Count, Is.EqualTo(2));
        // Assert.That(updates, Contains.Key(_accountId));
        // Assert.That(updates, Contains.Key(secondAccountId));
        // Assert.That(updates[_accountId].Count, Is.EqualTo(2));
        // Assert.That(updates[secondAccountId].Count, Is.EqualTo(2));
    }
    
    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void TestReceivePositionsSnapshot(int numberOfSsas)
    {
        _clock.CurrentInstant = Instant.FromUtc(2026, 3, 12, 12, 0);
        
        var c10001 = new Contract(10001, "Test",
            new(10000, "Test", SecurityType.Stock, new() { AssetId = 10000 }, 1, null, 10000, null, 1, 0.01m, null, 1, _usd,
                PnLCalculatorType.Default, null,
                null, null, new List<CommissionStructure>(), new List<TradingSession>(), new Exchange(),
                new Broker() { BrokerId = 100 },
                252, null),
            null, null, null, null, null, "ext-10001", null, null, new List<Stream>(), 100);
        
        var c10002 = new Contract(10002, "Test",
            new(10000, "Test", SecurityType.Stock, new() { AssetId = 10002 }, 1, null, 10000, null, 1, 0.01m, null, 1, _usd,
                PnLCalculatorType.Default, null,
                null, null, new List<CommissionStructure>(), new List<TradingSession>(), new Exchange(),
                new Broker() { BrokerId = 100 },
                252, null),
            null, null, null, null, null, "ext-10002", null, null, new List<Stream>(), 100);
        
        var c10003 = new Contract(10003, "Test",
            new(10000, "Test", SecurityType.Stock, new() { AssetId = 10003 }, 1, null, 10000, null, 1, 0.01m, null, 1, _usd,
                PnLCalculatorType.Default, null,
                null, null, new List<CommissionStructure>(), new List<TradingSession>(), new Exchange(),
                new Broker() { BrokerId = 100 },
                252, null),
            null, null, null, null, null, "ext-10003", null, null, new List<Stream>(), 100);
        
        _getContractByExternalId.Result.Add(_contractWithExternalId.ExternalContractId, _contractWithExternalId);
        _getContractByExternalId.Result.Add(c10001.ExternalContractId, c10001);
        _getContractByExternalId.Result.Add(c10002.ExternalContractId, c10002);
        _getContractByExternalId.Result.Add(c10003.ExternalContractId, c10003);
        
        _contractQueryHandler.Result.Add(_contractWithExternalId.ContractId, _contractWithExternalId);
        _contractQueryHandler.Result.Add(c10001.ContractId, c10001);
        _contractQueryHandler.Result.Add(c10002.ContractId, c10002);
        _contractQueryHandler.Result.Add(c10003.ContractId, c10003);
        
        _getSsaIdsForBrokerAccount.Result = Enumerable.Range(0, numberOfSsas).Select(i => 1000010 + i).ToList();
        var accounts = _getSsaIdsForBrokerAccount.Result.Select(CreateSsa).ToList();
        foreach (var account in accounts)
        {
            _getAccount.Result[account.Item2.AccountId] = account.Item2;
        }
        
        var t1 = new Trade("AS-test", _idsProvider.GetNextTradeId(), null, _brokerAccountId, 10000, null, null, null, null,
            null, Side.Buy, 10, 10000, 0, _clock.GetCurrentInstant(), null, null, null, 840, 1, 100000, null, null, false);
        var t2 = new Trade("AS-test", _idsProvider.GetNextTradeId(), null, _brokerAccountId, 10001, null, null, null, null,
            null, Side.Sell, 10, 20000, 0, _clock.GetCurrentInstant(), null, null, null, 840, 1, 200000, null, null, false);
        var t3 = new Trade("AS-test", _idsProvider.GetNextTradeId(), null, _brokerAccountId, 10002, null, null, null,
            null, null, Side.Buy, 3, 50000, 0, _clock.GetCurrentInstant(), null, null, null, 840, 1, 150000, null, null, false);
        
        _baState.Apply(new TradeEvt(_idsProvider.GetNextEventId(), _brokerAccountId, t1, _baState.GetNextVersion(), _clock.GetCurrentInstant(), 10000, 2, SecurityType.Stock, 2,
            PnLCalculatorType.Default, 0.01m, 0.01m, 1m), true);
        _baState.Apply(new TradeEvt(_idsProvider.GetNextEventId(), _brokerAccountId, t2, _baState.GetNextVersion(), _clock.GetCurrentInstant(), 10000, 2, SecurityType.Stock, 2,
            PnLCalculatorType.Default, 0.01m, 0.01m, 1m), true);
        _baState.Apply(new TradeEvt(_idsProvider.GetNextEventId(), _brokerAccountId, t3, _baState.GetNextVersion(), _clock.GetCurrentInstant(), 10000, 2, SecurityType.Stock, 2,
            PnLCalculatorType.Default, 0.01m, 0.01m, 1m), true);
        _internalTradesHistory.Events.Clear();
    
        var snapshot = new AccountPositionsSnapshot
        {
            AccountId = _brokerAccountId,
            Positions = 
            [
                // Matching position
                new ExternalPositionReport { AccountId = _brokerAccountId, BrokerId = 1, ExternalContractId = "ext-10000", OpenDt = _clock.GetCurrentInstant(), OpenPrice = 10000, SignedVolume = 10 },
                
                // Position with changed params
                new ExternalPositionReport { AccountId = _brokerAccountId, BrokerId = 1, ExternalContractId = "ext-10001", OpenDt = _clock.GetCurrentInstant(), OpenPrice = 19000, SignedVolume = -3 },
                
                // Missing position: p3
                
                // Unknown position:
                new ExternalPositionReport { AccountId = _brokerAccountId, BrokerId = 1, ExternalContractId = "ext-10003", OpenDt = _clock.GetCurrentInstant(), OpenPrice = 10000, SignedVolume = 7 },
            ],
            UpdateTs = _clock.GetCurrentInstant()
        };
        
        _ba.OnExternalAccountPositionsSnapshot(snapshot, _clock.GetCurrentInstant());
    
        var positions = _baState.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(3));
    
        var position = positions.SingleOrDefault(p => p.ContractId == 10000);
        Assert.That(position, Is.Not.Null);
        Assert.That(position is { SignedVolume: 10, OpenPrice: 10000 });
        
        position = positions.SingleOrDefault(p => p.ContractId == 10001);
        Assert.That(position, Is.Not.Null);
        Assert.That(position is { SignedVolume: -3, OpenPrice: 19000 });
        
        Assert.That(positions.SingleOrDefault(p => p.ContractId == 10002), Is.Null);
        
        position = positions.SingleOrDefault(p => p.ContractId == 10003);
        Assert.That(position, Is.Not.Null);
        Assert.That(position is { SignedVolume: 7, OpenPrice: 10000 });
        
        Assert.That(_internalTradesHistory.Events.Count, Is.EqualTo(4 + (numberOfSsas == 1 ? 4 : 0)));
        // Assert.That(_allocations.Events.Count, Is.EqualTo(numberOfSsas == 1 ? 4 : 0));
    }
    //
    // private Instant GetCurrentInstant() => SystemClock.Instance.GetCurrentInstant();
    //
    // private void AddDefaultContract()
    // {
    //     _contractExternalId.Contracts.Add(10000, 
    //         new Contract(new ContractDefinition { ContractId = 10000 }, new ContractTemplate { Broker = 1 }, 
    //             null, new() { Name = "USD", CurrencyId = 840 }, null, null, new(), 
    //             null, new Dictionary<long, TradingSession>(), null, null)
    //     );
    // }
    //
    // private void AddDefaultContractWithExternalId(long contractId = 10000)
    // {
    //     _contractExternalId.Contracts.Add(contractId,
    //         new Contract(new ContractDefinition { ContractId = contractId }, new ContractTemplate  { Broker = 1 },
    //             null, new() { Name = "USD", CurrencyId = 840 }, null, null, new(),
    //             null, new Dictionary<long, TradingSession>(), null, null)
    //         {
    //             ExternalContractId = $"ext-{contractId}", 
    //         }
    //     );
    // }
}