namespace QuantInfra.Common.Messaging
{
	public interface IEventHandler
	{
		void OnEvent(IMessage msg, long sequence, bool endOfBatch);
	}
}

