using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuantInfra.Common.Messaging;

namespace QuantInfra.Common.EventSourcing
{
	public class CommandResultWatcher : global::QuantInfra.Common.Messaging.IEventHandler
	{
		ConcurrentDictionary<Guid, TaskCompletionSource<object?>> _requests =
			new ConcurrentDictionary<Guid, TaskCompletionSource<object?>>();

		IListener _listener;
		IPublisher _publisher;


		public CommandResultWatcher(
			IListener listener,
			IPublisher publisher
		)
		{
			_listener = listener;
			_publisher = publisher;

			_listener.HandleEventsWith(this);
		}


		public Task<object?> ExecuteCommand(ICommand cmd)
		{
            var tcs = new TaskCompletionSource<object?>();
            _requests.TryAdd(cmd.RequestId, tcs);
			_publisher.PublishUnwrappedObject(cmd);
			return tcs.Task;
		}

        public void OnEvent(IMessage msg, long sequence, bool endOfBatch)
        {
            var message = msg.GetWrappedObject();
            if (message is CommandResult res)
            {
                OnCompletion(res);
            }
        }


        private void OnCompletion(CommandResult commandResult)
        {
            if (_requests.ContainsKey(commandResult.RequestId))
            {
                if (commandResult.IsSuccess)
                {
                    _requests[commandResult.RequestId].SetResult(commandResult.Result);
                }
                else
                {
                    _requests[commandResult.RequestId].SetException(
                        new Exception(commandResult.Error)
                    );
                }
                _requests.Remove(commandResult.RequestId, out var tcs);
            }
        }
    }
}

