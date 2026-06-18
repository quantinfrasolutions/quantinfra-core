using Common.Metrics;
using Disruptor.Dsl;
using NodaTime;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Common.Messaging.Patterns.TopicMulticast;

public class DisruptorPublishingSubscriber(Disruptor<IncomingDisruptorMessage> inputDisruptor, IClock clock)
    : IMulticastListenerEventHandler
{
    public void HandleIncomingMessage(DownstreamMessage message) =>
        inputDisruptor.PublishMessage(message, clock.GetCurrentInstant().ToUnixTimeMilliseconds(), MetricsUtils.GetUnixMicro());
}