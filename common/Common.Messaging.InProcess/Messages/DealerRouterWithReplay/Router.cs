using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Common.Messaging.InProcess.Messages.DealerRouterWithReplay;

public class Router : 
    IListener,
    IIncomingTransport,
    ITransport<QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.ControlMessage>
{
    private readonly string _serverName;
    
    private readonly Topology _topology;

    private bool _started = false;

    public Router(
        string serverName,
        Topology topology, 
        Disruptor<IncomingDisruptorMessage> disruptor, 
        ILoggerFactory loggerFactory,
        IReceiverStateProvider stateProvider, 
        IClock clock
    )
    {
        _serverName = serverName;
        ReceiverFilter = new(disruptor, this, loggerFactory.CreateLogger<ReceiverFilter>(), stateProvider, clock);
        _topology = topology;
        _topology.RegisterRouter(_serverName, this);
    }
    
    public ReceiverFilter ReceiverFilter { get; }

    public void SendMessage(QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.ControlMessage message)
    {
        _topology.SendControlMessageToDealer(_serverName, message.ClientId, message);
    }

    public void ReceiveMessage(ITransportMessage message, string? topicName = null)
    {
        if (!_started) return;
        if (message is not Patterns.DealerRouterWithReplay.DownstreamMessage dm)
            throw new NotSupportedException($"Received message of type {message.GetType()} not supported");
        
        ReceiverFilter.HandleIncomingMessage(dm);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _started = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _started = false;
        _topology.UnregisterRouter(_serverName);
        return Task.CompletedTask;
    }
}