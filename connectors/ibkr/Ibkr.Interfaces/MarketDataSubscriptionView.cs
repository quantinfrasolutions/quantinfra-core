namespace QuantInfra.Connectors.Ibkr.Interfaces
{
	public class MarketDataSubscriptionView
    {
	    public MarketDataSubscriptionView() { }
	    
		public MarketDataSubscriptionView(IbkrMarketDataSubscriptionListView? data, IbkrMarketDataSubscription? altData, int level = 0)
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
				Data = new IbkrMarketDataSubscriptionListView { SubscriptionId = altData.SubscriptionId };
			}
            
			if (!AreEqual(altData) && level < 1)
			{
				AltData = new MarketDataSubscriptionView(new IbkrMarketDataSubscriptionListView(altData, null), null, level + 1);
			}
		}
		public IbkrMarketDataSubscriptionListView? Data { get; set; }
        public MarketDataSubscriptionView? AltData { get; set; }
		public int Level { get; set; }
        
		private bool AreEqual(IbkrMarketDataSubscription alternativeData) => Data != null && alternativeData != null
			&& Data.ConId == alternativeData.ConId
			&& Data.Ticker == alternativeData.Ticker
			&& Data.SecurityType == alternativeData.SecurityType
			&& Data.Currency == alternativeData.Currency
			&& Data.Exchange == alternativeData.Exchange
			&& (
				Data.FuturesLastDateOrContractMonth == alternativeData.FuturesLastDateOrContractMonth
				|| (string.IsNullOrEmpty(Data.FuturesLastDateOrContractMonth) && (string.IsNullOrEmpty(alternativeData.FuturesLastDateOrContractMonth)))
			)
			&& Data.LocalSymbol == alternativeData.LocalSymbol
			&& Data.SubscriptionType == alternativeData.SubscriptionType
			&& Data.UseRTH == alternativeData.UseRTH
			&& Data.StreamId == alternativeData.StreamId;
    }
}

