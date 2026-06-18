using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Messaging.InProcess.Messages.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.InProcess.Messages.TopicMulticast;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Common.Messaging.InProcess;

public class TransportFactory(Topology topology, IClock clock, ILoggerFactory loggerFactory)
{
    public Dealer CreateDealer(string targetCompId, string senderCompId, Disruptor<IncomingDisruptorMessage> disruptor)
    {
        var transport = new Dealer(targetCompId, senderCompId, topology, disruptor, clock);
        return transport;
    }

    public Router CreateRouter(string serverName, Disruptor<IncomingDisruptorMessage> disruptor,
        IReceiverStateProvider stateProvider)
    {
        return new Router(serverName, topology, disruptor, loggerFactory, stateProvider, clock);
    }
    
    public MulticastTransport CreateMulticastTransport(string serverName, Disruptor<IncomingDisruptorMessage> disruptor) 
        => new(serverName, topology, disruptor, clock);
}