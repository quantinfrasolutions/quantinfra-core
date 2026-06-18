using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.Messaging;

namespace QuantInfra.Services.MonolithService;

public class Serializer([FromKeyedServices("serializer")] IMessageFactory messageFactory) : QuantInfra.Common.ServiceBase.Handlers.Serializer
{
    protected override object? ParseMessage(ITransportMessage msg)
    {
        if (msg is not QuantInfra.Common.Messaging.InProcess.IInProcessMessage tm)
            throw new NotSupportedException($"Message of type {msg.GetType()} is not supported");
        
        tm.Data = messageFactory.Parse(tm.Payload)!;
        return tm.Data;
    }

    protected override object? SerializeMessage(ITransportMessage msg)
    {
        if (msg is not QuantInfra.Common.Messaging.InProcess.IInProcessMessage tm)
            throw new NotSupportedException($"Message of type {msg.GetType()} is not supported");
        
        tm.Payload = tm.Data is not null ? messageFactory.SerializeAsString(tm.Data) : null;
        return tm.Data;
    }
}