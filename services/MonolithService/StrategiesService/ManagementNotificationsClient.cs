using Common.Metrics;
using Disruptor.Dsl;
using NodaTime;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.InProcess;
using QuantInfra.Common.Messaging.InProcess.Messages.TopicMulticast;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Events.Strategies.Management;
using TransportMessage = QuantInfra.Common.Messaging.InProcess.TransportMessage;

namespace QuantInfra.Services.MonolithService.StrategiesService;

public class ManagementNotificationsClient(StrategiesCore.Config config, Topology topology, Disruptor<IncomingDisruptorMessage> disruptor, IClock clock) : 
    Listener(disruptor, clock), 
    IManagementNotificationsClient,
    IIncomingTransport
{
    private readonly string _clientName = config.StrategiesServiceName;
    private readonly long _sessionId = clock.GetCurrentInstant().ToUnixTimeMilliseconds();
    private long _seqNo = 0;
    
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        topology.SubscribeToTopic("management", this);
    }

    public void PublishMessage(object message, Instant dt)
    {
        var swNow = MetricsUtils.GetUnixMicro();
        disruptor.PublishMessage(new TransportMessage("management", MessageType.DataMessage, _sessionId,
            _seqNo++, MetricsUtils.GetUnixMicro(), message), clock.GetCurrentInstant().ToUnixTimeMilliseconds(), swNow);
    }

    protected override bool CheckMessage(ITransportMessage message, string? topicName)
    {
        return message is TransportMessage { Data: StrategyCreatedEvt evt } && evt.Strategy.StrategyServiceName == _clientName;
    }
}