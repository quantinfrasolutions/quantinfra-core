using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Metrics;
using Disruptor.Dsl;
using NodaTime;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Sdk.StaticData;
using Quartz;

namespace QuantInfra.Services.MarketData.Jobs;

internal class GenerateBarWithConstantValueJob(
    Disruptor<IncomingDisruptorMessage> inputDisruptor,
    IClock clock
) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        var streams = (IReadOnlyCollection<ConstantStreamValue>)context.JobDetail.JobDataMap.Get("data");
        var closeDt = Instant.FromDateTimeUtc(context.ScheduledFireTimeUtc!.Value.UtcDateTime);
        var now = clock.GetCurrentInstant();
        
        foreach (var item in streams)
        {
            inputDisruptor.PublishParsedMessage(new StreamLastPriceUpdatedEvt(item.StreamId, (double)item.Value, closeDt), MetricsUtils.GetUnixMicro());
        }
        return Task.CompletedTask;
    }
}