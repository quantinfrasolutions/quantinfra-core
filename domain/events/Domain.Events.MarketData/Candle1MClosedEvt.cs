using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Domain.Events.MarketData
{
	public record struct Candle1MClosedEvt(ExchangeBar Bar, Instant Timestamp, long EventId = 0) : IEvent;
}

