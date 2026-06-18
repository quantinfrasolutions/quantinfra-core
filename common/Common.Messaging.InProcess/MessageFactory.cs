using Common.Messaging.InProcess;
using NodaTime;

namespace QuantInfra.Common.Messaging.InProcess;

public class MessageFactory : IMessageFactory
{
    public IMessage? WrapObject(object o) => new Message(o);

    public IMessage? CreateReceivedMessage(object o, Instant receiveTime) =>
        new Message(o, receiveTime);

    public string ContentType { get; } = "clr";
    
    public string SerializeAsString(object value)
    {
        throw new NotImplementedException();
    }

    public object? Parse(string? payload)
    {
        throw new NotImplementedException();
    }
}