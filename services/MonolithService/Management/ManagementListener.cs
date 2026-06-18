using Microsoft.Extensions.Hosting;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.InProcess;
using QuantInfra.Common.Messaging.InProcess.Messages.TopicMulticast;
using QuantInfra.Services.ManagementCore;
using IListener = QuantInfra.Common.Messaging.InProcess.IListener;

namespace QuantInfra.Services.MonolithService.Management;

public class ManagementListener(Topology topology, AccountsServiceResponseListener listener) : IListener, IHostedService
{
    public void ReceiveMessage(ITransportMessage message, string? topicName = null)
    {
        if (message is not DownstreamMessage msg)
            return;
            // throw new NotSupportedException($"Received message of type {message.GetType().Name} not supported");
        
        listener.HandleIncomingMessage(msg);
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        topology.SubscribeToTopic("responses", this);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}