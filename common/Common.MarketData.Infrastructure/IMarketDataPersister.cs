using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Common.MarketData.Infrastructure
{
	public interface IMarketDataPersister
	{		
		void AppendBAU(ExchangeBar bau, BarAggregationType aggType);
		Task AppendBAUAsync(ExchangeBar bar, BarAggregationType aggType);
		//void AppendTimeBAU(ExchangeBar bau);
		//void AppendFaceVolumeBAU(ExchangeBar bau);
		Instant GetLastPersistedOpenDt(int streamId);
	}
}

