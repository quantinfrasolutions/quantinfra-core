using QuantInfra.Binance.Futures.USDM.MarketData;
using QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;
using QuantInfra.Domain.Queries.MarketData;

namespace QuantInfra.Connectors.Binance.Futures.Usdm;

public class IncomingDisruptorMessage
{
    private byte[] _buffer = Array.Empty<byte>();
    private int _length;
    public long ReceivedAt;
    public long SwReceivedAt;

    public int? ConfirmedSubscriptionId = null;
    public IncomingMessageType Type;
    public Kline1m? Kline1m = null;
    public OrderBookSnapshot? OrderBookSnapshot = null;
    public OrderBookDiff? OrderbookDiff = null;
    public GetOrderBookSnapshot? GetOrderBookSnapshot = null;

    public void Set(byte[] buffer, int length, long receivedAt, long swReceivedAt)
    {
        _buffer = buffer;
        _length = length;
        ReceivedAt = receivedAt;
        SwReceivedAt = swReceivedAt;
    }

    public ReadOnlySpan<byte> Span => new(_buffer, 0, _length);

    public void Clear()
    {
        _buffer = Array.Empty<byte>();
        _length = 0;
    }


    public void SetSubscriptionConfirmation(int id) => ConfirmedSubscriptionId = id;
    
    public void SetKline1m(Kline1m kline1m)
    {
        Type = IncomingMessageType.Kline;
        Kline1m = kline1m;
    }

    public void SetOrderBookSnapshot(OrderBookSnapshot snapshot)
    {
        Type = IncomingMessageType.OrderBookSnapshot;
        OrderBookSnapshot = snapshot;
    }

    public void SetOrderBookUpdate(OrderBookDiff? diff)
    {
        Type = IncomingMessageType.OrderBookUpdate;
        OrderbookDiff = diff;
    }

    public void SetGetOrderBookSnapshot(GetOrderBookSnapshot request)
    {
        Type = IncomingMessageType.GetOrderBookSnapshot;
        GetOrderBookSnapshot = request;
    }
}

public enum IncomingMessageType
{
    Kline,
    OrderBookSnapshot,
    OrderBookUpdate,
    GetOrderBookSnapshot,
}