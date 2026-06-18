using System;
using Disruptor.Dsl;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Services.MarketData.Embedded;

public class InputDisruptorPublisher(Disruptor<IncomingDisruptorMessage> disruptor) : IPublisher, IPublisherFactory
{
    public void PublishUnwrappedObject(object o)
    {
        throw new NotImplementedException();
    }

    public void PublishUnwrappedObjectWithReceiptionSwMicro(object o, long swReceivedAt)
    {
        disruptor.PublishParsedMessage(o, swReceivedAt);
    }

    public void PublishUnwrappedString(Type type, string typeName, string data)
    {
        throw new NotImplementedException();
    }

    public void PublishWrappedMessage(IMessage message)
    {
        throw new NotImplementedException();
    }

    public IPublisher GetPublisher(string name)
    {
        if (name == "MarketDataServiceInput") return this;
        throw new NotSupportedException($"Publisher {name} is not supported. The only valid options is MarketDataServiceInput");
    }
    
    public void Dispose()
    {
        // do nothing
    }
}