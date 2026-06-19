using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.StaticData.InMemory;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Tests.Mocks;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Services.StrategiesCore.Tests;

[TestOf(typeof(StrategiesService))]
public class TestLoadStrategies
{
    private IServiceProvider _sp;
    private StrategiesService _ss;

    [SetUp]
    public void Setup()
    {
        _sp = MockService.BuildService(false);
        _ss = _sp.GetRequiredService<StrategiesService>();
        var sd = _sp.GetRequiredService<InMemoryStaticDataRepository>();
        sd.CreateCurrency(new Currency { CurrencyId = 840, Decimals = 2 });
    }

    [Test]
    public async Task TestLoadStrategy()
    {
        var currency = new Currency { CurrencyId = 840, Decimals = 2 };
        
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
        var asset = new Asset { AssetType = AssetType.Currency, AssetId = 840, Name = "USD" };
        sdRepository.CreateAsset(asset);
        var exchange = new Exchange();
        sdRepository.CreateExchange(exchange);
        var template = new ContractTemplate(10000, "TEST", SecurityType.Stock, null, 1, null, 100, null, 1, 0.01m, 0.01m, 1,
            currency, PLCalculatorType.Default, null, null, null, null, null,
            exchange, null, 252, null);
        sdRepository.CreateContractTemplate(template);
        var streams = new List<Stream>();
        var contract = new Contract(10000, "TEST", template, null, null, null, null, null, null, null, null,
            streams,  1);
        streams.Add(new() { Contract = contract, DatafeedId = 1, StreamId = 20000, Ticker = "Test", });
        sdRepository.CreateContract(contract);
        
        var strategiesRepo = (MockStrategyRecordsRepositoryReadonly)_sp.GetRequiredService<IStrategyRecordsRepositoryReadonly>();
        strategiesRepo.Strategies.Add(new Strategy()
        {
            AccountId = 100,
            ClassName = "QuantInfra.Tests.Mocks.TradeOnEveryBarStrategy",
            Name = "test strategy",
            Status = StrategyStatus.Running,
            RequiredBarStorages = new Dictionary<string, BarStorageConfig>()
            {
                {
                    "main", 
                    new BarStorageConfig()
                    {
                        IdType = IdType.Contract,
                        Id = 10000,
                        Timeframe = Period.FromMinutes(1),
                    }
                }
            },
            Symbols = new Dictionary<string, int>()
            {
                { "main", 10000 },
            },
            Params = "{ \"Side\": \"Buy\" }",
            StrategyId = 50,
            StrategyServiceName = "SS1",
        });
        
        var service = _sp.GetRequiredService<StrategiesService>();
        _ = service.StartAsync(CancellationToken.None);

        await Task.Delay(500);
        var api = _sp.GetRequiredService<MockAccountsServiceApi>();
        Assert.That(api.AccountStateSubscriptions.Count, Is.EqualTo(1));
        Assert.That(api.StrategyStateSubscriptions.Count, Is.EqualTo(1));
    }
}