using Disruptor.Dsl;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Common.ServiceBase;

public class DisruptorAsyncQueryBus : AsyncQueryBus
{
    private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;

    public DisruptorAsyncQueryBus(
        IQueryBus queryBus,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor
    ) : base(queryBus)
    {
        _outputDisruptor = outputDisruptor;
    }

    public override void SendAsyncQueryResponse<TRequest, TResult>(AsyncQueryResponse<TRequest, TResult> response)
    {
        _outputDisruptor.PublishMessage(response);
    }
}