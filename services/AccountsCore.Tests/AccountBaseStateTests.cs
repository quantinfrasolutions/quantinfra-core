using Common.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.StaticData.InMemory;
using QuantInfra.Domain.AccountRecordsStateManager;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Services.AccountsCore.State;
using QuantInfra.Tests.Mocks;

namespace QuantInfra.Services.AccountsCore.Tests;

[TestOf(typeof(Bpl))]
public class AccountBaseStateTests
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
        sd.CreateAsset(new Asset { AssetId = 840, Name = "test" });
    }
    
    [Test]
    public async Task TestNewBalanceOperations()
    {
        var currency = new Currency { CurrencyId = 840, Decimals = 2 };
        var state = _sp.GetRequiredService<AccountServiceState>();
        var account = new AccountRecordV6("AS", "test", 840,
            AccountType.VirtualAccount, PositionAccounting.Netted, null, true, true, null,
            100);
        var accS = AccountBaseState.CreateNewState(account, _sp.GetRequiredService<IEventBus>(), _sp.GetRequiredService<ILoggerFactory>());
        state.AccountRecords.Add(100, account);
        state.AccountStates.Add(100, accS);
        
        var repository = (MockAccountRecordsRepositoryReadonly)_sp.GetRequiredService<IAccountRecordsRepositoryReadonly>();
        repository.Accounts.Add(new AccountRecordV6()
        {
            AccountId = 100,
            AccountServiceName = "AS",
            AccountType = AccountType.VirtualAccount,
            CurrencyId = 840,
            Name = "test",
        });
        
        var sm = _sp.GetRequiredService<StateManager>();
        await sm.LoadAccountRecordsAsync(SystemClock.Instance.GetCurrentInstant());
        
        _bpl.Handle(new QuantInfra.Domain.Commands.Accounts.AccountsService.ProcessBalanceOperationCmd("AS", new()
        {
            AccountId = 100,
            AssetId = 840,
            Amount = 1000,
        }), false, SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());
        
        var balances = state.AccountStates[100].Balances;
        Assert.That(balances.Count, Is.EqualTo(1));
        Assert.That(balances, Contains.Key(840));
        Assert.That(balances[840], Is.EqualTo(1000));
    }
}