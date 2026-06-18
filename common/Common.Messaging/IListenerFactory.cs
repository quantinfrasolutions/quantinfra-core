namespace QuantInfra.Common.Messaging
{
	public interface IListenerFactory
	{
		IListener GetListener(string name);
		IMessageFactory MessageFactory { get; }
	}
}

