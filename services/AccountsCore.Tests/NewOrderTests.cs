using Common.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.StaticData.InMemory;
using QuantInfra.Domain.AccountRecordsStateManager;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.VirtualExecution;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Services.AccountsCore.State;
using QuantInfra.Tests.Mocks;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Services.AccountsCore.Tests;

[TestOf(typeof(Bpl))]
public class NewOrderTests
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
    }
    
    [Test]
    public async Task TestNewOrder()
    {
        var currency = new Currency { CurrencyId = 840, Decimals = 2 };
        var state = _sp.GetRequiredService<AccountServiceState>();
        var account = new AccountRecordV6("AS", "test", 840, AccountType.VirtualAccount,
            PositionAccounting.Netted, null, true, true, null, 100);
        state.AccountRecords.Add(100, account);
        state.AccountStates.Add(100, AccountBaseState.CreateNewState(account, _sp.GetRequiredService<IEventBus>(), _sp.GetRequiredService<ILoggerFactory>()));

        var ve = _sp.GetRequiredService<VirtualExecutor>();
        ve.Initialize(_sp.GetRequiredService<IQueryBus>());
        
        var repository = (MockAccountRecordsRepositoryReadonly)_sp.GetRequiredService<IAccountRecordsRepositoryReadonly>();
        repository.Accounts.Add(new AccountRecordV6()
        {
            AccountId = 100,
            AccountServiceName = "AS",
            AccountType = AccountType.VirtualAccount,
            CurrencyId = 840,
            Name = "test",
        });
        
        var sdRepository = _sp.GetRequiredService<InMemoryStaticDataRepository>();
        sdRepository.CreateAsset(new() { AssetType = AssetType.Currency, AssetId = 840, Name = "USD" });
        sdRepository.CreateCurrency(currency);
        var exchange = new Exchange();
        sdRepository.CreateExchange(exchange);
        var template = new ContractTemplate(10000, "TEST", SecurityType.Stock, null, 1, null, 100, null, 1, 0.01m, 0.01m, 1,
            currency, PnLCalculatorType.Default, null, null, null, null, null,
            exchange, null, 252, null);
        sdRepository.CreateContractTemplate(template);
        var contract = new Contract(10000, "TEST", template, null, null, null, null, null, null, null, null,
            new List<Stream>(), 100);
        sdRepository.CreateContract(contract);
        
        var sm = _sp.GetRequiredService<StateManager>();
        await sm.LoadAccountRecordsAsync(SystemClock.Instance.GetCurrentInstant());
        
        _bpl.Handle(new QuantInfra.Domain.Commands.Accounts.AccountsService.NewOrderCmd("AS", NewOrderSingle.MarketOrder("test-order", 100, 10000, null, PositionEffect.Unknown, 10, Side.Buy)), 
            false,SystemClock.Instance.GetCurrentInstant(), MetricsUtils.GetUnixMicro());
        
        Assert.That(state.AccountStates[100].Orders.Count(), Is.EqualTo(1));
    }
}