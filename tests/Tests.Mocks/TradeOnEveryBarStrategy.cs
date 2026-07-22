using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Tests.Mocks;

public class TradeOnEveryBarStrategyConfig
{
    public Side Direction { get; set; }
    public decimal? TradeSize { get; set; }
}

public class TradeOnEveryBarStrategy : AbstractSingleBarHostedStrategy<TradeOnEveryBarStrategyConfig>
{
    public TradeOnEveryBarStrategy(Strategy sc) : base(sc)
    {
    }

    protected override void CalculateVector(string barQualifier, StrategyCalculationContext context)
    {
        if (context.AccountState.Positions.Any())
        {
            ClosePosition();
            return;
        }

        var side = Params.Direction == (Side.Buy & Side.Sell)
            ? new Random().NextDouble() > 0.5 ? Side.Buy : Side.Sell
            : Params.Direction == Side.Buy
                ? Side.Buy
                : Params.Direction == Side.Sell
                    ? Side.Sell
                    : throw new InvalidOperationException("Side must be provided");

        if (Params.TradeSize.HasValue)
        {
            OpenPosition(Params.TradeSize.Value * side.GetSign());
        }
        else
        {
            OpenPosition("main", GetVolume() * side.GetSign());
        }
    }

    protected override void OnInitialized(StrategyInitializationContext context)
    {
        RegisterIndicator(new Close());
    }
}