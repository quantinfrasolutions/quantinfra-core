using System;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Services.MarketData.TradeAggregators
{
    public class VolumeTradeAggregator : AbstractTradeAggregator
    {
        public VolumeTradeAggregator(double volume)
        {
            Volume = volume;
        }


        public double Volume { get; }
        

        public override ExchangeBar AppendTrade(ExchangeTrade trade)
        {
            // Method needs to return array of bars, in case a single trade's volume is bigger than
            // the aggregation unit
            throw new NotImplementedException();


            //if (Aggregation == null)
            //{
            //    StartNewAggregation(trade.Dt, trade.Price, 0);
            //    return null;
            //}

            //if (trade.Volume + Aggregation.Volume < Volume)
            //{
            //    UpdateCurrentAggregation(trade.Price, trade.Volume);
            //    return null;
            //}

            //var remainingVolume = trade.Volume;

            //do
            //{
            //    var volumeToUse = Math.Min(remainingVolume, Volume);
            //    var currentBarCloseVol = Volume - Aggregation.Volume;
            //    UpdateCurrentAggregation(trade.Price, currentBarCloseVol);
            //    Aggregation.CloseDt = trade.Dt;
            //    var res = Aggregation;
            //}
            //StartNewAggregation(trade.Dt, trade.Price, trade.Volume - currentBarCloseVol);
            //return res;
        }

        public override ExchangeBar[] AppendBar(ExchangeBar bar)
        {
            // Method needs to return array of bars, in case a single trade's volume is bigger than
            // the aggregation unit
            throw new NotImplementedException();

            //if (Aggregation == null)
            //{
            //    StartNewAggregation(bar);
            //    return null;
            //}

            //if (trade.Volume + Aggregation.Volume < Volume)
            //{
            //    UpdateCurrentAggregation(trade.Price, trade.Volume);
            //    return null;
            //}

            //var currentBarCloseVol = Volume - Aggregation.Volume;
            //UpdateCurrentAggregation(trade.Price, currentBarCloseVol);
            //Aggregation.CloseDt = trade.Dt;
            //var res = Aggregation;
            //StartNewAggregation(trade.Dt, trade.Price, trade.Volume - currentBarCloseVol);
            //return res;
        }
    }
}
