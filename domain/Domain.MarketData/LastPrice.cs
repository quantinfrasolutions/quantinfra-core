using NodaTime;

namespace QuantInfra.Domain.MarketData;

public class LastPrice
{
    public LastPrice(decimal price, Instant timestamp)
    {
        Price = price;
        Timestamp = timestamp;
    }
    
    public decimal Price { get; }
    public Instant Timestamp { get; }
}