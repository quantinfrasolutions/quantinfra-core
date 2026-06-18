using System;
using System.Threading.Tasks;
using QuantInfra.Services.AccountsCore.EventHandlers;
using Quartz;

namespace QuantInfra.Services.AccountsCore.Jobs
{
	public class HeartbeatJob : IJob
	{
		private readonly IOutputToInputDisruptorPublisher _publisher;

		public HeartbeatJob(IOutputToInputDisruptorPublisher publisher)
		{
			_publisher = publisher;
		}

        public Task Execute(IJobExecutionContext context)
        {
	        _publisher.PublishMessage("HeartbeatJob", new ProcessHeartbeatCmd(Guid.NewGuid()));
	        
	        return Task.CompletedTask;
        }
	}
}

