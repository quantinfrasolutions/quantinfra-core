using NodaTime;

namespace QuantInfra.Domain.Events.MarketData
{
	public record struct StreamLastPriceUpdatedEvt(int StreamId, double Price, Instant ReferenceDt);
}

