namespace QuantInfra.Connectors.Binance.Common
{
	public class BinanceUsdmMarketDataSubscriptionView
    {
	    public BinanceUsdmMarketDataSubscriptionView() { }
	    
		public BinanceUsdmMarketDataSubscriptionView(BinanceUsdmMarketDataSubscriptionListView? data, BinanceUsdmMarketDataSubscription? altData, int level = 0)
		{
			Level = level;
			if (data != null)
			{
				Data = data;
				if (level == 0 && altData != null)
				{
					data.LastBar = altData.LastBar;
				}
			}
			else if (altData != null)
			{
				Data = new BinanceUsdmMarketDataSubscriptionListView { SubscriptionId = altData.SubscriptionId };
			}
            
			if (!AreEqual(altData) && level < 1)
			{
				AltData = new BinanceUsdmMarketDataSubscriptionView(new BinanceUsdmMarketDataSubscriptionListView(altData, null), null, level + 1);
			}
		}
		public BinanceUsdmMarketDataSubscriptionListView? Data { get; set; }
        public BinanceUsdmMarketDataSubscriptionView? AltData { get; set; }
		public int Level { get; set; }
        
		private bool AreEqual(BinanceUsdmMarketDataSubscription alternativeData) => Data != null && alternativeData != null
			&& Data.SubscriptionType == alternativeData.SubscriptionType
			&& Data.StreamId == alternativeData.StreamId
			&& Data.Symbol == alternativeData.Symbol;
    }
}

