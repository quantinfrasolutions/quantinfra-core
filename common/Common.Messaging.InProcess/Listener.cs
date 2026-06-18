using Common.Metrics;
using Disruptor.Dsl;
using Microsoft.Extensions.Hosting;
using NodaTime;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Common.Messaging.InProcess;

public class Listener(Disruptor<IncomingDisruptorMessage> disruptor, IClock clock) : IIncomingTransport, IListener, IHostedService
{
    private bool _started = false;
    
    public void ReceiveMessage(ITransportMessage message, string? topicName = null)
    {
        if (!_started) return;
        var receivedAt = clock.GetCurrentInstant().ToUnixTimeMilliseconds();
        var swReceivedAt = MetricsUtils.GetUnixMicro();
        if (!CheckMessage(message, topicName)) return;
        
        using var scope = disruptor.PublishEvent();
        var data = scope.Event();
        data.ReceiveMessage(message, receivedAt, swReceivedAt, false);
        if (message is IInProcessMessage msg)
        {
            data.SetParsedMessage(msg.Data);
        }
    }

    protected virtual bool CheckMessage(ITransportMessage message, string? topicName) => true;

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        _started = true;
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        _started = false;
        return Task.CompletedTask;
    }
}