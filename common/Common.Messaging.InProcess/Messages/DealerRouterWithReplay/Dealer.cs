using Disruptor.Dsl;
using NodaTime;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Common.Messaging.InProcess.Messages.DealerRouterWithReplay;

public class Dealer :
    Listener,
    ITransport<QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.DownstreamMessage>
{
    private readonly string _targetCompId;
    private readonly string _senderCompId;
    private readonly Topology _topology;

    public Dealer(string targetCompId, string senderCompId, Topology topology, Disruptor<IncomingDisruptorMessage> disruptor, IClock clock) : base(disruptor, clock)
    {
        _targetCompId = targetCompId;
        _senderCompId = senderCompId;
        _topology = topology;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        _topology.RegisterDealer(_targetCompId, _senderCompId, this);
    }

    public void SendMessage(Patterns.DealerRouterWithReplay.DownstreamMessage message)
    {
        _topology.SendMessageToRouter(_targetCompId, message);
    }
}