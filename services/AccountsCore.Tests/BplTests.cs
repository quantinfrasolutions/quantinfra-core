using Common.Accounts.Abstractions;
using Common.Metrics;
using Disruptor.Dsl;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.StaticData.InMemory;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Domain.Account.Execution.State;
using QuantInfra.Domain.AccountRecordsStateManager;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Domain.StrategyRecordsStateManager;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Services.AccountsCore.State;
using QuantInfra.Tests.Mocks;
using GetAccountState = QuantInfra.Services.AccountsCore.QueryHandlers.GetAccountState;
using Strategy = QuantInfra.Sdk.Strategies.Strategy;

namespace QuantInfra.Services.AccountsCore.Tests;

[TestOf(typeof(Bpl))]
public class BplTests
{
    private Bpl _bpl;
#pragma warning disable NUnit1032
    private IServiceProvider _sp;
#pragma warning restore NUnit1032

    [SetUp]
    public void Setup()
    {
        _sp = MockService.BuildService();
        _bpl = _sp.GetRequiredService<Bpl>();
        var sd = _sp.GetRequiredService<InMemoryStaticDataRepository>();
        sd.CreateCurrency(new Currency { CurrencyId = 840, Decimals = 2 });
        sd.CreateBroker(new() { BrokerId = 1, BrokerType = BrokerType.Ibkr, });
    }

    [Test]
    [TestCase(AccountType.VirtualAccount, "AS", true)]
    [TestCase(AccountType.BrokerAccount, "AS", true)]
    [TestCase(AccountType.StrategySubAccount, "AS", true)]
    // [TestCase(AccountType.ExecutableSubAccount, "AS",true)]
    // [TestCase(AccountType.Account, "other", false)]
    // [TestCase(AccountType.Fund, "other", false)]
    // [TestCase(AccountType.ClientSubAccount, "other", false)]
    public async Task TestAccountCreatedMessageReceived(AccountType accountType, string serviceName, bool mustBeCreated)
    {
        var sm = _sp.GetRequiredService<StateManager>();
        await sm.LoadAccountRecordsAsync(SystemClock.Instance.GetCurrentInstant());
        
        var currency = new Currency { CurrencyId = 840, Decimals = 2 };
        
        var msg = new AccountCreatedEvt(
            1000,
            100,
            new AccountRecordV6()
            {
                AccountId = 100,
                AccountServiceName = serviceName, 
                AccountType = accountType,
                CurrencyId = 840,
                Name = "test",
                BrokerId = accountType == AccountType.BrokerAccount ? 1 : null,
            },
            SystemClock.Instance.GetCurrentInstant()
        );
        
        _bpl.Handle(msg, false,SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());

        var queryBus = _sp.GetRequiredService<IQueryBus>();
        if (mustBeCreated)
        {
            var accountRecord = queryBus.Query<GetAccount, AccountRecordV6>(new(100));
            Assert.That(accountRecord is
            {
                AccountServiceName: "AS",
                Name: "test",
            });
            Assert.That(accountRecord.CurrencyId, Is.EqualTo(840));
            Assert.That(accountRecord.AccountType, Is.EqualTo(accountType));
            
            var accountState = queryBus.Query<GetAccountState, AccountBaseState?>(new(100));
            Assert.That(accountState, Is.Not.Null);
            Assert.That(accountState.Balances, Is.Empty);
            Assert.That(accountState.Version, Is.EqualTo(0));

            if (accountType == AccountType.VirtualAccount || accountType == AccountType.StrategySubAccount)
            {
                Assert.That(accountState, Is.InstanceOf<AccountBaseState>());
            }
            if (accountType == AccountType.BrokerAccount)
            {
                Assert.That(accountState, Is.InstanceOf<BrokerAccountState>());
            }
        }
        else
        {
            Assert.Throws<KeyNotFoundException>(() => queryBus.Query<GetAccount, AccountRecordV6?>(new(100)));
        }
    }

    [Test]
    public async Task TestLoadAccounts()
    {
        var repository = (MockAccountRecordsRepositoryReadonly)_sp.GetRequiredService<IAccountRecordsRepositoryReadonly>();
        repository.Accounts.Add(new AccountRecordV6()
        {
            AccountId = 100,
            AccountServiceName = "AS", 
            AccountType = AccountType.VirtualAccount,
            CurrencyId = 840,
            Name = "test",
        });
        
        repository.Accounts.Add(new AccountRecordV6()
        {
            AccountId = 110,
            AccountServiceName = "AS", 
            AccountType = AccountType.StrategySubAccount,
            CurrencyId = 840,
            Name = "test SSA",
        });
        
        repository.Accounts.Add(new AccountRecordV6()
        {
            AccountId = 120,
            AccountServiceName = "AS", 
            AccountType = AccountType.BrokerAccount,
            BrokerId = 1,
            CurrencyId = 840,
            Name = "test BA",
        });
        
        var accRecMgr = _sp.GetRequiredService<StateManager>();
        await accRecMgr.LoadAccountRecordsAsync(SystemClock.Instance.GetCurrentInstant());

        var strRecMgr = _sp.GetRequiredService<AccountsServiceStateManager>();
        await strRecMgr.LoadStrategiesRecordsAsync(SystemClock.Instance.GetCurrentInstant());
        

        _bpl.OnStateInitializedInternal();
        var manager = _sp.GetRequiredService<MockManagementNotificationsClient>();
        Assert.That(manager.Messages.Count, Is.EqualTo(3));
        _bpl.Handle(manager.Messages[0], false,SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());
        _bpl.Handle(manager.Messages[1], false,SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());
        _bpl.Handle(manager.Messages[2], false,SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());
        
        var queryBus = _sp.GetRequiredService<IQueryBus>();
        var accountRecord = queryBus.Query<GetAccount, AccountRecordV6?>(new(100));
        Assert.That(accountRecord is
        {
            AccountServiceName: "AS",
            Name: "test"
        });
        
        accountRecord = queryBus.Query<GetAccount, AccountRecordV6?>(new(110));
        Assert.That(accountRecord is
        {
            AccountServiceName: "AS",
            Name: "test SSA"
        });
        
        accountRecord = queryBus.Query<GetAccount, AccountRecordV6?>(new(120));
        Assert.That(accountRecord is
        {
            AccountServiceName: "AS",
            Name: "test BA"
        });
        
        var accountState = queryBus.Query<GetAccountState, AccountBaseState?>(new(100));
        Assert.That(accountState, Is.Not.Null);
        Assert.That(accountState.Balances, Is.Empty);
        Assert.That(accountState.Version, Is.EqualTo(0));
        
        accountState = queryBus.Query<GetAccountState, AccountBaseState?>(new(110));
        Assert.That(accountState, Is.Not.Null);
        Assert.That(accountState.Balances, Is.Empty);
        Assert.That(accountState.Version, Is.EqualTo(0));
        
        accountState = queryBus.Query<GetAccountState, AccountBaseState?>(new(120));
        Assert.That(accountState, Is.Not.Null);
        Assert.That(accountState.Balances, Is.Empty);
        Assert.That(accountState.Version, Is.EqualTo(0));
    }
    
    [Test]
    public async Task TestStrategiesAccounts()
    {
        var repository = (MockAccountRecordsRepositoryReadonly)_sp.GetRequiredService<IAccountRecordsRepositoryReadonly>();
        repository.Accounts.Add(new AccountRecordV6()
        {
            AccountId = 100,
            AccountServiceName = "AS", 
            AccountType = AccountType.VirtualAccount,
            CurrencyId = 840,
            Name = "test",
        });
        
        var strategiesRepo = (MockStrategyRecordsRepositoryReadonly)_sp.GetRequiredService<IStrategyRecordsRepositoryReadonly>();
        strategiesRepo.Strategies.Add(new Strategy()
        {
            AccountId = 100,
            ClassName = "test",
            Name = "test strategy",
            Status = StrategyStatus.Running,
            RequiredBarStorages = new Dictionary<string, BarStorageConfig>(),
            Symbols = new Dictionary<string, int>(),
            Params = "",
            StrategyId = 50,
            StrategyServiceName = "SS1",
        });
        
        var manager = _sp.GetRequiredService<MockManagementNotificationsClient>();
        var accRecMgr = _sp.GetRequiredService<StateManager>();
        await accRecMgr.LoadAccountRecordsAsync(SystemClock.Instance.GetCurrentInstant());
        Assert.That(manager.Messages.Count, Is.EqualTo(1));
        _bpl.Handle(manager.Messages[0], false,SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());
        manager.Messages.Clear();
        
        var strRecMgr = _sp.GetRequiredService<AccountsServiceStateManager>();
        await strRecMgr.LoadStrategiesRecordsAsync(SystemClock.Instance.GetCurrentInstant());
        

        _bpl.OnStateInitializedInternal();
        Assert.That(manager.Messages.Count, Is.EqualTo(1));
        _bpl.Handle(manager.Messages[0], false,SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());
        
        var queryBus = _sp.GetRequiredService<IQueryBus>();
        var strategy = queryBus.Query<GetStrategyRecord, Strategy?>(new(50));
        Assert.That(strategy is
        {
            Name: "test strategy"
        });
        
        var accountState = queryBus.Query<GetAccountState, AccountBaseState?>(new(100));
        Assert.That(accountState, Is.Not.Null);
        Assert.That(accountState.Balances, Is.Empty);
        Assert.That(accountState.Version, Is.EqualTo(0));
    }
    
    [Test]
    [TestCase("AS", true)]
    [TestCase("other", false)]
    public async Task TestStrategyCreatedMessageReceived(string serviceName, bool mustBeCreated)
    {
        if (serviceName == "AS")
        {
            var state = _sp.GetRequiredService<AccountServiceState>();
            state.AccountRecords.Add(100, new AccountRecordV6()
            {
                AccountId = 100,
                AccountServiceName = serviceName,
                AccountType = AccountType.VirtualAccount,
                CurrencyId = 840,
                Name = "test",
            });
        }

        var sm = _sp.GetRequiredService<StateManager>();
        await sm.LoadAccountRecordsAsync(SystemClock.Instance.GetCurrentInstant());
        var strRecMgr = _sp.GetRequiredService<AccountsServiceStateManager>();
        await strRecMgr.LoadStrategiesRecordsAsync(SystemClock.Instance.GetCurrentInstant());
        
        var msg = new StrategyCreatedEvt(
            1000,
            50,
            new()
            {
                AccountId = 100,
                ClassName = "test",
                Name = "test strategy",
                RequiredBarStorages = new Dictionary<string, BarStorageConfig>(),
                Symbols = new Dictionary<string, int>(),
                Params = "",
                StrategyId = 50,
            },
            new()
            {
                AccountId = 100,
                AccountServiceName = serviceName, 
                AccountType = AccountType.VirtualAccount,
                CurrencyId = 840,
            },
            SystemClock.Instance.GetCurrentInstant()
        );
        
        _bpl.Handle(msg, false,SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());

        var queryBus = _sp.GetRequiredService<IQueryBus>();
        if (mustBeCreated)
        {
            var strategyRecord = queryBus.Query<GetStrategyRecord, Strategy?>(new(50));
            Assert.That(strategyRecord is
            {
                Name: "test strategy",
            });
            
            var strategyState = queryBus.Query<GetStrategyState, IStrategyStateReadonly?>(new(50));
            Assert.That(strategyState, Is.Not.Null);
            Assert.That(strategyState.ActiveSignalGroup, Is.Null);
            Assert.That(strategyState.Version, Is.EqualTo(0));
        }
        else
        {
            var strategyRecord = queryBus.Query<GetStrategyRecord, Strategy?>(new(50));
            Assert.That(strategyRecord, Is.Null);
            
            var strategyState = queryBus.Query<GetStrategyState, IStrategyStateReadonly?>(new(50));
            Assert.That(strategyState, Is.Null);
        }
    }

    [Test]
    [TestCase(100, true, true)]
    [TestCase(100, true, false)]
    [TestCase(200, false, true)]
    [TestCase(200, false, false)]
    [TestCase(120, true, false, true)]
    public async Task TestAccountStateQuery(int accountId, bool resultNotEmpty, bool useMulticast, bool expectBrokerAccountState = false)
    {
        await TestLoadAccounts();
        
        _bpl.Handle(new QuantInfra.Domain.Queries.Accounts.AccountsService.GetAccountState(Guid.NewGuid(), "AS", accountId, useMulticast),
            false,SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());

        var outputDisruptor = _sp.GetRequiredService<Disruptor<OutgoingDisruptorMessage>>();
        var msg = outputDisruptor.RingBuffer[outputDisruptor.Cursor - 1]; // the last message is SYNC
        Assert.That(msg.Value, Is.Not.Null);
        if (expectBrokerAccountState)
        {
            var response = msg.Value as
                AsyncQueryResponse<GetBrokerAccountState, BrokerAccountStateReadonly?>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Result, resultNotEmpty ? Is.Not.Null : Is.Null);
            Assert.That(response.UseMulticast, Is.EqualTo(useMulticast));
        }
        else
        {
            var response = msg.Value as
                AsyncQueryResponse<QuantInfra.Domain.Queries.Accounts.AccountsService.GetAccountState, AccountStateReadonly?>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Result, resultNotEmpty ? Is.Not.Null : Is.Null);
            Assert.That(response.UseMulticast, Is.EqualTo(useMulticast));
        }
    }

    [Test]
    public async Task TestAccountStateAsyncQuery()
    {
        await TestLoadAccounts();
        
        _bpl.Handle(new QuantInfra.Domain.Queries.Accounts.AccountsService.GetAccountState(100, "AS"), false, 
            SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());
        
        var outputDisruptor = _sp.GetRequiredService<Disruptor<OutgoingDisruptorMessage>>();
        var msg = outputDisruptor.RingBuffer[outputDisruptor.Cursor - 1]; // the last message is SYNC
        Assert.That(msg.Value, Is.Not.Null);
        Assert.That(msg.Value, Is.TypeOf<AsyncQueryResponse<QuantInfra.Domain.Queries.Accounts.AccountsService.GetAccountState, AccountStateReadonly?>>());
        
        _bpl.Handle(new QuantInfra.Domain.Queries.Accounts.AccountsService.GetBrokerAccountState(120, "AS"), false, 
            SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());
        msg = outputDisruptor.RingBuffer[outputDisruptor.Cursor - 1]; // the last message is SYNC
        Assert.That(msg.Value, Is.Not.Null);
        Assert.That(msg.Value, Is.TypeOf<AsyncQueryResponse<QuantInfra.Domain.Queries.Accounts.AccountsService.GetBrokerAccountState, BrokerAccountStateReadonly?>>());
        Assert.That(((AsyncQueryResponse<QuantInfra.Domain.Queries.Accounts.AccountsService.GetBrokerAccountState, BrokerAccountStateReadonly?>)msg.Value).Result, Is.Not.Null);
        
        _bpl.Handle(new QuantInfra.Domain.Queries.Accounts.AccountsService.GetBrokerAccountState(100, "AS"), false, 
            SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());
        msg = outputDisruptor.RingBuffer[outputDisruptor.Cursor - 1]; // the last message is SYNC
        Assert.That(msg.Value, Is.Not.Null);
        Assert.That(msg.Value, Is.TypeOf<AsyncQueryResponse<QuantInfra.Domain.Queries.Accounts.AccountsService.GetBrokerAccountState, BrokerAccountStateReadonly?>>());
        Assert.That(((AsyncQueryResponse<QuantInfra.Domain.Queries.Accounts.AccountsService.GetBrokerAccountState, BrokerAccountStateReadonly?>)msg.Value).Result, Is.Null);
    }
}