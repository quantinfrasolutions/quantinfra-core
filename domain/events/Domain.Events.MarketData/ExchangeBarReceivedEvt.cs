using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Domain.Events.MarketData;

public record struct ExchangeBarReceivedEvt(ExchangeBar Bar);// : IEvent    