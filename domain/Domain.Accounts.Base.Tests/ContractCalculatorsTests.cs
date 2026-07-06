using System.Collections;
using Domain.Accounts.Base.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Domain.Accounts.Base.Tests;

[TestFixture]
public class ContractCalculatorsTests
{
    class TestData
    {
        public static Asset StockAsset = new() { AssetId = 100000, Name = "Stock asset", AssetType =  AssetType.Stock };
        public static Currency Usd = new() { CurrencyId = 840, Asset = new() { AssetId = 840, Name = "USD", AssetType = AssetType.Currency }, Decimals = 2 };
        public static Currency Eur = new() { CurrencyId = 978, Asset = new() { AssetId = 978, Name = "EUR", AssetType = AssetType.Currency }, Decimals = 2 };
        public static Currency Btc = new() { CurrencyId = 1000, Asset = new() { AssetId = 1000, Name = "BTC", AssetType = AssetType.Currency }, Decimals = 8 };

        private static ContractTemplate _stock = new ContractTemplate(10000, "Stock", SecurityType.Stock, StockAsset, 1, null,
            100000, null, 1, 0.01m, null, 1, Usd, 
            PLCalculatorType.Default, null, null, null, new List<CommissionStructure>(),
            new List<TradingSession>(), new Exchange(), new Broker(), 252, null);
        
        private static ContractTemplate _directFutures = new ContractTemplate(10000, "Futures", SecurityType.Futures, Btc.Asset, 1, null,
            100000, null, 1, 1, 5, 1, Usd, 
            PLCalculatorType.DefaultFutures, null, null, null, new List<CommissionStructure>(),
            new List<TradingSession>(), new Exchange(), new Broker(), 252, null);
        
        private static ContractTemplate _inverseFutures = new ContractTemplate(10000, "Inverse Futures", SecurityType.Futures, Btc.Asset, 1, null,
            100000, null, 1, 1, null, 1, Btc, 
            PLCalculatorType.InverseFutures, null, null, null, new List<CommissionStructure>(),
            new List<TradingSession>(), new Exchange(), new Broker(), 252, null);
        
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData(new Input(_stock, Side.Buy, 840, 10, 10000, 0, 10 * 10000, 10050))
                    .Returns(new Result(10, 0, 10 * 10000,
                        10050 * 10, 10050 * 10, 10050 * 10,
                        -10000 * 10, 10000 * 10, 50  * 10, 0, 50 * 10, 50 * 10
                    ));
                yield return new TestCaseData(new Input(_stock, Side.Sell, 840, 10, 10000, 0, 10 * 10000, 10050))
                    .Returns(new Result(-10, 0, 10 * 10000,
                        -10050 * 10, -10050 * 10, -10050 * 10,
                        10000 * 10, -10000 * 10, -50  * 10, 0, -50 * 10, -50 * 10
                    ));
                yield return new TestCaseData(new Input(_directFutures, Side.Buy, 840, 10, 100, 0, 10 * 100 * 5, 95))
                    .Returns(new Result(10, 0, 10 * 100 * 5,
                        10 * 95 * 5, 10 * 95 * 5, (95 - 100) * 10 * 5,
                        0, 0, 0, (95 - 100) * 10 * 5, (95 - 100) * 10 * 5, (95 - 100) * 10 * 5
                    ));
                yield return new TestCaseData(new Input(_directFutures, Side.Sell, 840, 10, 100, 0, 10 * 100 * 5, 95))
                    .Returns(new Result(-10, 0, 10 * 100 * 5,
                        -10 * 95 * 5, -10 * 95 * 5, -(95 - 100) * 10 * 5,
                        0, 0, 0, -(95 - 100) * 10 * 5, -(95 - 100) * 10 * 5, -(95 - 100) * 10 * 5
                    ));
                yield return new TestCaseData(new Input(_inverseFutures, Side.Buy, 1000, 5000, 50000, 0, Math.Round(5000m / 50000, 8), 52000))
                    .Returns(new Result(5000, 0, Math.Round(5000m / 50000, 8),
                        Math.Round(5000m / 52000, 8), Math.Round(5000m / 52000, 8), Math.Round(5000m / 52000m - 5000m / 50000, 8),
                        0, 0, 0, Math.Round(5000m / 52000m - 5000m / 50000, 8), 
                        Math.Round(5000m / 52000m - 5000m / 50000, 8), Math.Round(5000m / 52000m - 5000m / 50000, 8)
                    ));
                yield return new TestCaseData(new Input(_inverseFutures, Side.Sell, 1000, 5000, 50000, 0, Math.Round(5000m / 50000, 8), 52000))
                    .Returns(new Result(-5000, 0, Math.Round(5000m / 50000, 8),
                        -Math.Round(5000m / 52000, 8), -Math.Round(5000m / 52000, 8), -Math.Round(5000m / 52000m - 5000m / 50000, 8),
                        0, 0, 0, -Math.Round(5000m / 52000m - 5000m / 50000, 8), 
                        -Math.Round(5000m / 52000m - 5000m / 50000, 8), -Math.Round(5000m / 52000m - 5000m / 50000, 8)
                    ));
            }
        }
    }

    public record Input(
        ContractTemplate ContractTemplate,
        Side Side,
        int AccountCurrency,
        decimal Volume,
        decimal Price,
        decimal Commission,
        decimal CalculatedCcyLastQty,
        decimal MtmPrice
    );

    public record struct Result(decimal SignedVolume, decimal OpenPrice, decimal TotalOpenPayments,
        decimal SignedValue, decimal SignedValueInAccountCcy, decimal EquityValueInAccountCcy,
        decimal CashBalance, decimal Holdings, decimal UnrealizedPnL, decimal FuturesVariationMargin, decimal TotalBalance, decimal TotalValue);
    
    [TestCaseSource(typeof(TestData), nameof(TestData.TestCases))]
    public Result TestPositionPnL(Input input)
    {
        var events = new GenericMockEventHandler();
        var projections = new MockProjectionHandler();

        var data = new StaticData();

        var contract = new Contract(
            10000,
            "TEST",
            input.ContractTemplate,
            null, null, null, null, null,
            "test-id",
            null,
            null, new List<Stream>(),
            1
        );
        data.Contracts.Add(10000, contract);
        
        data.Currencies.Add(TestData.Usd.CurrencyId, TestData.Usd);
        data.Currencies.Add(TestData.Eur.CurrencyId, TestData.Eur);
        data.Currencies.Add(TestData.Btc.CurrencyId, TestData.Btc);

        var serviceProvider = new ServiceCollection()
            .UseSingletonInMemoryBus()
            .AddLogging(c =>
            {
                c.ClearProviders();
                c.AddFilter("*", LogLevel.Debug).AddConsole();
            })
            .AddSingleton<IEventHandler>(events)
            .AddSingleton<IQueryHandler<GetContract, Contract>>(data)
            .AddSingleton<IQueryHandler<GetCurrency, Currency>>(data)
            .AddSingleton<IQueryHandler<GetConversionRate, decimal?>>(data)
            .AddSingleton<IQueryHandler<GetConversionPath, IReadOnlyCollection<FxConversionStep>>>(data)
            .AddSingleton<IProjectionWriter>(projections)
            
            .BuildServiceProvider();
        
        var accountRecord = new AccountRecordV6("AS", "Test account USD", input.AccountCurrency, AccountType.VirtualAccount,
            PositionAccounting.Netted, null, true, true, null);

        var stateUsd = AccountBaseState.CreateNewState(accountRecord,
            serviceProvider.GetRequiredService<IEventBus>(), serviceProvider.GetRequiredService<ILoggerFactory>());

        var accountUsd = new AccountBase(
            accountRecord,
            stateUsd,
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
        
        accountUsd.ProcessTrade(
            new Trade("AS", 100000, null, accountRecord.AccountId, 10000, null, null, null, null, null,
                input.Side, input.Volume, input.Price, input.Commission, Instant.FromUtc(2026, 7, 3, 16, 0),
                null, null, null, contract.Template.SettlementCurrency.CurrencyId, 1, input.CalculatedCcyLastQty, 
                null, null, false
            ),
            Instant.FromUtc(2026, 7, 3, 16, 0));

        var positions = stateUsd.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));

        var position = positions.Single();

        var mtm = accountUsd.MarkToMarket(new Dictionary<int, decimal>() { { 10000, input.MtmPrice } },
            Instant.FromUtc(2026, 7, 3, 16, 1));
        
        Assert.That(mtm.positionValues.Count, Is.EqualTo(1));
        Assert.That(mtm.positionValues, Contains.Key(position.OpenTradeId));
        var pv = mtm.positionValues[position.OpenTradeId];
        
        Assert.That(mtm.balanceValues.Count, Is.EqualTo(1));
        Assert.That(mtm.balanceValues, Contains.Key(input.ContractTemplate.SettlementCurrency.CurrencyId));
        var bv = mtm.balanceValues[input.ContractTemplate.SettlementCurrency.CurrencyId];

        return new(position.SignedVolume, 0 /*position.OpenPrice TODO*/, position.TotalOpenPayments,
            pv.SignedValue, pv.SignedValueInAccountCcy, pv.EquityValueInAccountCcy,
            bv.CashBalance, bv.Holdings, bv.UnrealizedPnL, bv.FuturesVariationMargin, bv.TotalBalance, bv.TotalValue);
    }
}