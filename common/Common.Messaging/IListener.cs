using System;

namespace QuantInfra.Common.Messaging
{
	public interface IListener : IDisposable
	{
		void HandleEventsWith(IEventHandler handler);
		IMessageFactory MessageFactory { get; }
	}
}

