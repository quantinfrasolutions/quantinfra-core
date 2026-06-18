using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Domain.Events.MarketData;

public record struct ExchangeTradeReceivedEvt// : IEvent
{        
    public ExchangeTradeReceivedEvt(ExchangeTrade trade)
    {
        Trade = trade;
    }
             

    public ExchangeTrade Trade { get; }

    public override string ToString() => $"ExchangeTradeReceivedEvt|Trade={Trade}";
}