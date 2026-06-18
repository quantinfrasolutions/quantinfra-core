using System;
using NodaTime;
using QuantInfra.Common.MarketData;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Services.MarketData.TradeAggregators
{
    public abstract class AbstractTradeAggregator
    {
        protected AggregatingBar? Aggregation { get; set; }
        public abstract ExchangeBar AppendTrade(ExchangeTrade trade);
        public abstract ExchangeBar[] AppendBar(ExchangeBar bar);

        protected void StartNewAggregation(int streamId, int? contractId, Instant dt, double price, double volume,
            int datasourceId, int? tradingSessionId)
        {
            Aggregation = new(streamId, contractId, dt, Instant.MinValue, price, price, price, price, volume, 0, datasourceId, tradingSessionId);
        }

        protected void StartNewAggregation(Instant dt, ExchangeBar bar)
        {
            Aggregation = new(bar.StreamId, bar.ContractId, dt, Instant.MinValue, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, bar.DollarValue, bar.DatasourceId, bar.TradingSessionId);
        }

        protected ExchangeBar CloseAggregation(Instant closeDt)
        {
            var res = Aggregation;
            res!.CloseDt = closeDt;
            Aggregation = null;
            return res.ToExchangeBar();
        }

        protected void UpdateCurrentAggregation(double price, double volume)
        {
            Aggregation!.High = Math.Max(price, Aggregation.High);
            Aggregation.Low = Math.Min(price, Aggregation.Low);
            Aggregation.Close = price;
            Aggregation.Volume += volume;
        }

        protected void UpdateCurrentAggregation(ExchangeBar bar)
        {
            Aggregation!.High = Math.Max(Aggregation.High, bar.High);
            Aggregation.Low = Math.Min(Aggregation.Low, bar.Low);
            Aggregation.Close = bar.Close;
            Aggregation.Volume += bar.Volume;
        }

        internal ExchangeBar? GetCurrentAggregation() => Aggregation?.ToExchangeBar();
    }
}
