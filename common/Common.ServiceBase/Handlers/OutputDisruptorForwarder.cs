using Common.Metrics;
using Disruptor.Dsl;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Common.ServiceBase.Handlers;

public class Forwarder(Disruptor<OutgoingDisruptorMessage> outputDisruptor)
{
    protected void Forward<T>(T e)
    {
        outputDisruptor.PublishMessage(e, swPublishedAt: MetricsUtils.GetUnixMicro());
    }
}

public class ForwardToOutputDisruptorEventHandler(Disruptor<OutgoingDisruptorMessage> outputDisruptor)
    : Forwarder(outputDisruptor), IEventHandler, IProjectionWriter
{
    public void Handle(IEvent e) => Forward(e);
    public void Write(IProjectionUpdatedEvent e) => Forward(e);
}

public class ForwardToOutputDisruptorCmdHandler<T>(Disruptor<OutgoingDisruptorMessage> outputDisruptor)
    : Forwarder(outputDisruptor), ICommandHandler<T>
    where T : ICommand
{
    public virtual void Handle(T e) => Forward(e);
}