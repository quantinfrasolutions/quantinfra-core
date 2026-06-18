using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Services.ExecutionCore;

public class RequestSnapshotHandler(
    Disruptor<IncomingDisruptorMessage> disruptor, 
    IMessageFactory messageFactory,
    IClock clock,
    ILogger<RequestSnapshotHandler> logger
) : IRequestSnapshotMessageHandler
{
    public void Handle(RequestSnapshotMessage message)
    {
        throw new NotImplementedException();
        // var msg = message.Topic.Split('.');
        // if (msg.Length != 2 || msg[0] != "e" || !int.TryParse(msg[1], out var id))
        // {
        //     logger.LogWarning($"Invalid topic format: {message.Topic}");
        //     return;
        // }
        // var now = clock.GetCurrentInstant().ToUnixTimeMilliseconds();
        // var request = new GetExternalAccountSnapshot(id, true);
        //
        // disruptor.PublishMessage(
        //     new TransportMessage("RequestSnapshotHandler", 
        //         messageFactory.SerializeAsString(request),
        //         now
        //     ),
        //     now,
        //     MetricsUtils.GetUnixMicro()
        // );
    }
}