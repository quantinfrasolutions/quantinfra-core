using QuantInfra.Ibkr.Interfaces;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Connectors.Ibkr.Interfaces
{
	public interface IIbkrActiveSubscriptionsRepository : 
		IActiveSubscriptionsRepository<IbkrMarketDataSubscriptionRequest, IbkrMarketDataSubscription, IbkrMarketDataSubscriptionListView>
	{
        
    }
}

