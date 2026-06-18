using Disruptor.Dsl;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Common.ServiceBase;

public class AsyncRequestsUniversalHandler(Disruptor<OutgoingDisruptorMessage> outputDisruptor) : IAsyncQueryHandler
{
    public void Handle(IAsyncQuery query)
    {
        outputDisruptor.PublishMessage(query);
    }
}