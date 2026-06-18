using System;
using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Services.MarketData.TradeAggregators
{
    public class MinuteTradeAggregator : AbstractTradeAggregator
    {
        public override ExchangeBar AppendTrade(ExchangeTrade trade)
        {
            throw new NotImplementedException();
            // if (Aggregation == null)
            // {
            //     StartNewAggregation(trade.StreamId, trade.ContractId, GetMinuteOpeningTime(trade.Dt), trade.Price, trade.Volume, trade.DatasourceId, trade.TradingSessionId);
            //     Aggregation.CloseDt = Aggregation.OpenDt.PlusTicks(600000000); // 1 minute
            //     return null;
            // }
            // if (trade.Dt > Aggregation.CloseDt)
            // {
            //     var res = CloseAggregation(Aggregation.CloseDt);
            //     StartNewAggregation(TODO, TODO, GetMinuteOpeningTime(trade.Dt), trade.Price, trade.Volume, TODO, TODO);                                
            //     return res;
            // }
            //
            // UpdateCurrentAggregation(trade.Price, trade.Volume);
            // return null;
        }

        public override ExchangeBar[] AppendBar(ExchangeBar bar)
        {
            ExchangeBar res = null;
            if (AppendBar(bar, out res))
            {
                ExchangeBar res2 = null;
                AppendBar(bar, out res2);

                if (res2 != null)
                    return new ExchangeBar[] { res, res2 };
            }

            if (res != null)
                return new ExchangeBar[] { res };

            return new ExchangeBar[0];
        }

        private bool AppendBar(ExchangeBar bar, out ExchangeBar res)
        {
            if (bar.CloseDt.ToDateTimeUtc().Second % 5 != 0)
            {
                throw new NotImplementedException("Only 5 second bars are supported now");
            }
            //var currentMinute =;
            var update = true;
            if (Aggregation == null)
            {
                StartNewAggregation(GetMinuteOpeningTime(bar.OpenDt), bar);
                Aggregation.CloseDt = Aggregation.OpenDt.Plus(Duration.FromMinutes(1));
                update = false;
            }
            if (bar.CloseDt == Aggregation.CloseDt)
            {
                if (update)
                    UpdateCurrentAggregation(bar);
                res = CloseAggregation(Aggregation.CloseDt);
                return false;
            }
            if (bar.OpenDt >= Aggregation.CloseDt)
            {
                res = CloseAggregation(Aggregation.CloseDt);
                return true;
            }

            UpdateCurrentAggregation(bar);
            res = null;
            return false;
        }

        private Instant GetMinuteOpeningTime(Instant i) =>
            Instant.FromUnixTimeSeconds((long)(i.ToUnixTimeSeconds() / 60) * 60);
    }
}
