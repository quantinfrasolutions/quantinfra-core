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
            PnLCalculatorType.Default, null, null, null, new List<CommissionStructure>(),
            new List<TradingSession>(), new Exchange(), new Broker(), 252, null);
        
        private static ContractTemplate _directFutures = new ContractTemplate(10000, "Futures", SecurityType.Futures, Btc.Asset, 1, null,
            100000, null, 1, 1, 5, 1, Usd, 
            PnLCalculatorType.Futures, null, null, null, new List<CommissionStructure>(),
            new List<TradingSession>(), new Exchange(), new Broker(), 252, null);
        
        private static ContractTemplate _inverseFutures = new ContractTemplate(10000, "Inverse Futures", SecurityType.Futures, Btc.Asset, 1, null,
            100000, null, 1, 1, null, 1, Btc, 
            PnLCalculatorType.InverseFutures, null, null, null, new List<CommissionStructure>(),
            new List<TradingSession>(), new Exchange(), new Broker(), 252, null);
        
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData(new Input("Stock long profit", _stock, Side.Buy, 840, 10, 10000, 20, 10 * 10000, 10050, 10050 * 10))
                    .Returns(new Result(10, 10000, 10 * 10000,
                        10050 * 10, 10050 * 10, 10050 * 10,
                        -10000 * 10 - 20, 10000 * 10, 50  * 10, 0, 50 * 10 - 20, 50 * 10 - 20,
                        -10000 * 10 - 20, 50 * 10 - 20
                    ));
                yield return new TestCaseData(new Input("Stock short profit", _stock, Side.Sell, 840, 10, 10000, 20, 10 * 10000, 9500, 9500 * 10))
                    .Returns(new Result(-10, 10000, 10 * 10000,
                        -9500 * 10, -9500 * 10, -9500 * 10,
                        10000 * 10 - 20, -10000 * 10, 500  * 10, 0, 500 * 10 - 20, 500 * 10 - 20,
                        10000 * 10 - 20, 500 * 10 - 20
                    ));
                yield return new TestCaseData(new Input("Stock long loss", _stock, Side.Buy, 840, 10, 10000, 20, 10 * 10000, 9500, 9500 * 10))
                    .Returns(new Result(10, 10000, 10 * 10000,
                        9500 * 10, 9500 * 10, 9500 * 10,
                        -10000 * 10 - 20, 10000 * 10, -500  * 10, 0, -500 * 10 - 20, -500 * 10 - 20,
                        -10000 * 10 - 20, -500 * 10 - 20
                    ));
                yield return new TestCaseData(new Input("Stock short loss", _stock, Side.Sell, 840, 10, 10000, 20, 10 * 10000, 10050, 10050 * 10))
                    .Returns(new Result(-10, 10000, 10 * 10000,
                        -10050 * 10, -10050 * 10, -10050 * 10,
                        10000 * 10 - 20, -10000 * 10, -50  * 10, 0, -50 * 10 - 20, -50 * 10 - 20,
                        10000 * 10 - 20, -50 * 10 - 20
                    ));
                yield return new TestCaseData(new Input("Futures long loss", _directFutures, Side.Buy, 840, 10, 100, 20, 10 * 100 * 5, 95, 10 * 95 * 5))
                    .Returns(new Result(10, 100, 10 * 100 * 5,
                        10 * 95 * 5, 10 * 95 * 5, (95 - 100) * 10 * 5,
                        -20, 0, 0, (95 - 100) * 10 * 5, (95 - 100) * 10 * 5 - 20, (95 - 100) * 10 * 5 - 20,
                        -20, (95 - 100) * 10 * 5 - 20
                    ));
                yield return new TestCaseData(new Input("Futures short profit", _directFutures, Side.Sell, 840, 10, 100, 20, 10 * 100 * 5, 95, 10 * 95 * 5))
                    .Returns(new Result(-10, 100, 10 * 100 * 5,
                        -10 * 95 * 5, -10 * 95 * 5, -(95 - 100) * 10 * 5,
                        -20, 0, 0, -(95 - 100) * 10 * 5, -(95 - 100) * 10 * 5 - 20, -(95 - 100) * 10 * 5 - 20,
                        -20, -(95 - 100) * 10 * 5 - 20
                    ));
                yield return new TestCaseData(new Input("Futures long profit", _directFutures, Side.Buy, 840, 10, 100, 20, 10 * 100 * 5, 105, 10 * 105 * 5))
                    .Returns(new Result(10, 100, 10 * 100 * 5,
                        10 * 105 * 5, 10 * 105 * 5, (105 - 100) * 10 * 5,
                        -20, 0, 0, (105 - 100) * 10 * 5, (105 - 100) * 10 * 5 - 20, (105 - 100) * 10 * 5 - 20,
                        -20, (105 - 100) * 10 * 5 - 20
                    ));
                yield return new TestCaseData(new Input("Futures short loss", _directFutures, Side.Sell, 840, 10, 100, 20, 10 * 100 * 5, 105, 10 * 105 * 5))
                    .Returns(new Result(-10, 100, 10 * 100 * 5,
                        -10 * 105 * 5, -10 * 105 * 5, -(105 - 100) * 10 * 5,
                        -20, 0, 0, -(105 - 100) * 10 * 5, -(105 - 100) * 10 * 5 - 20, -(105 - 100) * 10 * 5 - 20,
                        -20, -(105 - 100) * 10 * 5 - 20
                    ));
                yield return new TestCaseData(new Input("Inverse futures long profit", _inverseFutures, Side.Buy, 1000, 5000, 50000, 0.0001m, Math.Round(5000m / 50000, 8), 52000, Math.Round(5000m / 52000, 8)))
                    .Returns(new Result(5000, 50000, Math.Round(5000m / 50000, 8),
                        Math.Round(5000m / 52000, 8), Math.Round(5000m / 52000, 8), Math.Round(5000m / 50000 - 5000m / 52000m, 8),
                        -0.0001m, 0, 0, Math.Round(5000m / 50000 - 5000m / 52000m, 8), 
                        Math.Round(5000m / 50000 - 5000m / 52000m, 8) - 0.0001m, Math.Round(5000m / 50000 - 5000m / 52000m, 8) - 0.0001m,
                        -0.0001m, Math.Round(5000m / 50000 - 5000m / 52000m, 8) - 0.0001m
                    ));
                yield return new TestCaseData(new Input("Inverse futures short loss", _inverseFutures, Side.Sell, 1000, 5000, 50000, 0.0001m, Math.Round(5000m / 50000, 8), 52000, Math.Round(5000m / 52000, 8)))
                    .Returns(new Result(-5000, 50000, Math.Round(5000m / 50000, 8),
                        -Math.Round(5000m / 52000, 8), -Math.Round(5000m / 52000, 8), -Math.Round(5000m / 50000 - 5000m / 52000m, 8),
                        -0.0001m, 0, 0, -Math.Round(5000m / 50000m - 5000m / 52000, 8), 
                        -Math.Round(5000m / 50000 - 5000m / 52000m, 8) - 0.0001m, -Math.Round(5000m / 50000 - 5000m / 52000m, 8) - 0.0001m,
                        -0.0001m, -Math.Round(5000m / 50000 - 5000m / 52000m, 8) - 0.0001m
                    ));
                yield return new TestCaseData(new Input("Inverse futures long loss", _inverseFutures, Side.Buy, 1000, 5000, 50000, 0.0001m, Math.Round(5000m / 50000, 8), 48000, Math.Round(5000m / 48000, 8)))
                    .Returns(new Result(5000, 50000, Math.Round(5000m / 50000, 8),
                        Math.Round(5000m / 48000, 8), Math.Round(5000m / 48000, 8), Math.Round(5000m / 50000 - 5000m / 48000, 8),
                        -0.0001m, 0, 0, Math.Round(5000m / 50000 - 5000m / 48000, 8), 
                        Math.Round(5000m / 50000 - 5000m / 48000, 8) - 0.0001m, Math.Round(5000m / 50000 - 5000m / 48000, 8) - 0.0001m,
                        -0.0001m, Math.Round(5000m / 50000 - 5000m / 48000, 8) - 0.0001m
                    ));
                yield return new TestCaseData(new Input("Inverse futures short profit", _inverseFutures, Side.Sell, 1000, 5000, 50000, 0.0001m, Math.Round(5000m / 50000, 8), 48000, Math.Round(5000m / 48000, 8)))
                    .Returns(new Result(-5000, 50000, Math.Round(5000m / 50000, 8),
                        -Math.Round(5000m / 48000, 8), -Math.Round(5000m / 48000, 8), -Math.Round(5000m / 50000 - 5000m / 48000, 8),
                        -0.0001m, 0, 0, -Math.Round(5000m / 50000 - 5000m / 48000, 8), 
                        -Math.Round(5000m / 50000 - 5000m / 48000, 8) - 0.0001m, -Math.Round(5000m / 50000 - 5000m / 48000, 8) - 0.0001m,
                        -0.0001m, -Math.Round(5000m / 50000 - 5000m / 48000, 8) - 0.0001m
                    ));
            }
        }
    }

    public record Input(
        string Name,
        ContractTemplate ContractTemplate,
        Side Side,
        int AccountCurrency,
        decimal Volume,
        decimal Price,
        decimal Commission,
        decimal CalculatedCcyLastQty,
        decimal MtmPrice,
        decimal CalculatedCcyLastQtySecondTrade
    );

    public record struct Result(decimal SignedVolume, decimal OpenPrice, decimal TotalOpenPayments,
        decimal SignedValue, decimal SignedValueInAccountCcy, decimal EquityValueInAccountCcy,
        decimal CashBalance, decimal Holdings, decimal UnrealizedPnL, decimal FuturesVariationMargin, decimal TotalBalance, decimal TotalValue,
        decimal BalanceAfterFirstTrade, decimal BalanceAfterSecondTrade);
    
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

        var accountState = AccountBaseState.CreateNewState(accountRecord,
            serviceProvider.GetRequiredService<IEventBus>(), serviceProvider.GetRequiredService<ILoggerFactory>());

        var account = new AccountBase(
            accountRecord,
            accountState,
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
        
        account.ProcessTrade(
            new Trade("AS", 100000, null, accountRecord.AccountId, 10000, null, null, null, null, null,
                input.Side, input.Volume, input.Price, input.Commission, Instant.FromUtc(2026, 7, 3, 16, 0),
                null, null, null, contract.Template.SettlementCurrency.CurrencyId, 1, input.CalculatedCcyLastQty, 
                null, null, false
            ),
            Instant.FromUtc(2026, 7, 3, 16, 0));

        Assert.That(accountState.Balances.Count, Is.EqualTo(1));
        Assert.That(accountState.Balances, Contains.Key(contract.Template.SettlementCurrency.CurrencyId));
        var balanceAfterFirstTrade = accountState.Balances[input.ContractTemplate.SettlementCurrency.CurrencyId];
        
        var positions = accountState.Positions.ToList();
        Assert.That(positions.Count, Is.EqualTo(1));

        var position = positions.Single();

        var mtm = account.MarkToMarket(new Dictionary<int, decimal>() { { 10000, input.MtmPrice } },
            Instant.FromUtc(2026, 7, 3, 16, 1));
        
        Assert.That(mtm.positionValues.Count, Is.EqualTo(1));
        Assert.That(mtm.positionValues, Contains.Key(position.OpenTradeId));
        var pv = mtm.positionValues[position.OpenTradeId];
        
        Assert.That(mtm.balanceValues.Count, Is.EqualTo(1));
        Assert.That(mtm.balanceValues, Contains.Key(input.ContractTemplate.SettlementCurrency.CurrencyId));
        var bv = mtm.balanceValues[input.ContractTemplate.SettlementCurrency.CurrencyId];
        
        account.ProcessTrade(
            new Trade("AS", 100001, null, accountRecord.AccountId, 10000, null, null, null, null, null,
                input.Side.Invert(), input.Volume, input.MtmPrice, 0, Instant.FromUtc(2026, 7, 3, 16, 1),
                null, null, null, contract.Template.SettlementCurrency.CurrencyId, 1, input.CalculatedCcyLastQtySecondTrade, 
                null, null, false
            ),
            Instant.FromUtc(2026, 7, 3, 16, 1));
        
        Assert.That(accountState.Balances.Count, Is.EqualTo(1));
        Assert.That(accountState.Balances, Contains.Key(contract.Template.SettlementCurrency.CurrencyId));
        var balanceAfterSecondTrade = accountState.Balances[input.ContractTemplate.SettlementCurrency.CurrencyId];

        return new(position.SignedVolume, position.OpenPrice, position.TotalOpenPayments,
            pv.SignedValue, pv.SignedValueInAccountCcy, pv.EquityValueInAccountCcy,
            bv.CashBalance, bv.Holdings, bv.UnrealizedPnL, bv.FuturesVariationMargin, bv.TotalBalance, bv.TotalValue,
            balanceAfterFirstTrade, balanceAfterSecondTrade);
    }
}