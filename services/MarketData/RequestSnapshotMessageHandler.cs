using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Domain.Queries.MarketData;

namespace QuantInfra.Services.MarketData;

public class RequestSnapshotMessageHandler(IMarketDataSnapshotsProvider publisher) : IRequestSnapshotMessageHandler
{
    public void Handle(RequestSnapshotMessage message)
    {
        var parts = message.Topic.Split('.');
        if (parts.Length == 3 && parts[1] == TopicDefinitions.OrderBookUpdatesTopicPrefix)
        {
            var cid= int.Parse(parts[2]);
            publisher.RequestOrderBookSnapshot(new(cid, true));
        }
    }
}

public interface IMarketDataSnapshotsProvider
{
    void RequestOrderBookSnapshot(GetOrderBookSnapshot request);
}