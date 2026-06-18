using AccountsCore;
using Common.Metrics;
using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Strategies.AccountsService;

namespace QuantInfra.Services.AccountsCore;

public class RequestSnapshotHandler(
    Config config,
    Disruptor<IncomingDisruptorMessage> disruptor, 
    IMessageFactory messageFactory,
    IClock clock,
    ILogger<RequestSnapshotHandler> logger
) : IRequestSnapshotMessageHandler
{
    private readonly string _serviceName = config.AccountServiceName;
    
    public void Handle(RequestSnapshotMessage message)
    {
        var msg = message.Topic.Split('.');
        if (msg.Length != 2 || !int.TryParse(msg[1], out var id))
        {
            logger.LogWarning($"Invalid topic format: {message.Topic}");
            return;
        }
        var now = clock.GetCurrentInstant().ToUnixTimeMilliseconds();
        object request;
        if (msg[0] == TopicDefinitions.AccountUpdatesTopicPrefix)
        {
            request = new GetAccountState(id, _serviceName, true);
        }
        else if (msg[0] == TopicDefinitions.StrategyUpdatesTopicPrefix)
        {
            request = new GetStrategyState(id, true);
        }
        else return;
        
        disruptor.PublishMessage(
            new TransportMessage("RequestSnapshotHandler", 
                messageFactory.SerializeAsString(request),
                now
            ),
            now,
            MetricsUtils.GetUnixMicro()
        );
    }
}