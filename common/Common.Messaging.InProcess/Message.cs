using NodaTime;
using QuantInfra.Common.Messaging;

namespace Common.Messaging.InProcess;

public class Message : IMessage
{
    private readonly object _payload;
    
    public Message(object payload)
    {
        _payload = payload;
    }

    public Message(object payload, Instant receiveTime) : this(payload)
    {
        ReceivedAt = receiveTime;
    }
    
    public object? GetWrappedObject() => _payload;

    public byte[] GetBytes()
    {
        throw new NotImplementedException();
    }

    public Instant ReceivedAt { get; }
    public string GetString()
    {
        throw new NotImplementedException();
    }
}