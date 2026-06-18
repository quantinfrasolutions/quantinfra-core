using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;

namespace QuantInfra.Services.MarketData.Embedded;

public class MockReceiverStateProvider : IReceiverStateProvider
{
    public ReceiverState GetReceiverState()
    {
        throw new System.NotImplementedException();
    }

    public void UpdateState(string senderCompId, long sessionId, long sequenceNumber) { }
}