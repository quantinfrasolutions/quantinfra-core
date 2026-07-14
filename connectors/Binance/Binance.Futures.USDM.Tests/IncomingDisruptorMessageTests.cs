using System.Text;
using QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Tests;

public class IncomingDisruptorMessageTests
{
    [Test]
    public void SetSubscriptionConfirmation_CarriesParsedRequestIdAsServiceAckEvent()
    {
        var json = Encoding.UTF8.GetBytes("""{"result":null,"id":17}""");
        var kind = BinanceMessageRouter.Classify(json, out var service);
        var message = new IncomingDisruptorMessage();

        message.SetSubscriptionConfirmation(service.Id!.Value);

        Assert.Multiple(() =>
        {
            Assert.That(kind, Is.EqualTo(BinanceMsgKind.ServiceAck));
            Assert.That(message.Type, Is.EqualTo(IncomingMessageType.ServiceAck));
            Assert.That(message.ConfirmedSubscriptionId, Is.EqualTo(17));
        });
    }
}
