using NodaTime;

namespace QuantInfra.Common.Messaging;

public interface IMessageFactory
{
	IMessage? WrapObject(object o);
	IMessage? CreateReceivedMessage(object o, Instant receiveTime);
	string ContentType { get; }
	string SerializeAsString(object value);
	object? Parse(string? payload);
}