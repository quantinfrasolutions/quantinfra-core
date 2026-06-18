namespace QuantInfra.Common.Messaging;
using NodaTime;

public interface IMessage
{
	object? GetWrappedObject();
	byte[] GetBytes();
	Instant ReceivedAt { get; }
	string GetString();
}