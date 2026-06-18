namespace QuantInfra.Common.Messaging;

public interface IPublisherFactory
{
	IPublisher GetPublisher(string name);
}