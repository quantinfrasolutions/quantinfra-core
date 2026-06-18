using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Domain.Commands.Accounts.AccountsService;
using Quartz;

namespace QuantInfra.Services.AccountsCore.Jobs
{
	public class EndOfDayJob(
		IOutputToInputDisruptorPublisher publisher,
		ILogger<EndOfDayJob> logger,
		Config config
	) : IJob
	{
		public Task Execute(IJobExecutionContext context) => Task.Run(() =>
		{
			logger.LogInformation("Sending an EndOfDayCmd");
			publisher.PublishMessage("EndOfDayJob", 
				new RunEndOfDayCmd(
					config.AccountServiceName, 
					Instant.FromDateTimeUtc(context.ScheduledFireTimeUtc!.Value.UtcDateTime)
				));
		});
	}
}

