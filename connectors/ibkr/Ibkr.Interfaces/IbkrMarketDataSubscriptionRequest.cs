using QuantInfra.Connectors.Ibkr.Interfaces;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Ibkr.Interfaces
{
	public class IbkrMarketDataSubscriptionRequest
	{
		public IbkrMarketDataSubscriptionRequest() { }
		
		public IbkrMarketDataSubscriptionRequest(
			int conId,
			string ticker,
			SecType securityType,
			string currency,
			string exchange,
			string futuresLastDateOrContractMonth,
			string localSymbol,
			SubscriptionType subscriptionType,
			bool useRth,
			int? streamId,
			bool suppressValidations,
			bool outsideOfTradingHours
		)
		{
			ConId = conId;
			Ticker = ticker;
			SecurityType = securityType;
			Currency = currency;
			Exchange = exchange;
			FuturesLastDateOrContractMonth = futuresLastDateOrContractMonth;
			LocalSymbol = localSymbol;
			SubscriptionType = subscriptionType;
			UseRTH = useRth;
			StreamId = streamId;
			SuppressValidations = suppressValidations;
			OutsideOfTradingHours = outsideOfTradingHours;
		}

		public int? Id { get; set; }
		public int ConId { get; set; }
		public string Ticker { get; set; }
		public SecType SecurityType { get; set; }
		public string Currency { get; set; }
		public string Exchange { get; set; }
		public string? FuturesLastDateOrContractMonth { get; set; } = "";
		public string LocalSymbol { get; set; }        
		public SubscriptionType SubscriptionType { get; set; }
		public bool UseRTH { get; set; }
		public int? StreamId { get; set; }
		public bool SuppressValidations { get; set; }
		public bool OutsideOfTradingHours { get; set; }
	}
}

