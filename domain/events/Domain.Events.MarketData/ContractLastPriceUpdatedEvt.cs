using NodaTime;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Events.MarketData
{
	public record ContractLastPriceUpdatedEvt(
		int ContractId,
		decimal Price,
		int? TradingSessionId,
		Instant ReferenceDt,
		Instant Timestamp,
		long EventId = 0
	) : IEvent;
}

