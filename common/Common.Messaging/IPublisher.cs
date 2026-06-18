using System;

namespace QuantInfra.Common.Messaging;

public interface IPublisher : IDisposable
{
	void PublishUnwrappedObject(object o);
	void PublishUnwrappedObjectWithReceiptionSwMicro(object o, long swReceivedAt);
	void PublishUnwrappedString(Type type, string typeName, string data);
	void PublishWrappedMessage(IMessage message);
}