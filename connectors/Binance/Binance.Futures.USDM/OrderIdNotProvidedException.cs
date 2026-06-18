namespace Binance.Futures.USDM;

public class OrderIdNotProvidedException : InvalidOperationException
{
    public OrderIdNotProvidedException(string message) : base(message) { }
}