namespace Binance.Futures.USDM;

public class InvalidModifyRequestException : Exception
{
    public InvalidModifyRequestException()
    {
    }

    public InvalidModifyRequestException(string? message) : base(message)
    {
    }
}