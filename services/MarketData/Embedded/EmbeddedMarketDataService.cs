using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Services.MarketData.Embedded;

public class EmbeddedMarketDataService : IHostedService
{
    private readonly Bpl _bpl;
    private readonly Disruptor<IncomingDisruptorMessage> _inputDisruptor;
    private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;
    private readonly IEnumerable<IIncomingTransport> _incomingTransports;

    public EmbeddedMarketDataService(
        Config config,
        Bpl bpl,
        IComponentExceptionHandler exceptionHandler,
        MulticastSender multicast,
        Disruptor<IncomingDisruptorMessage> inputDisruptor,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor,
        Persister persister,
        ILoggerFactory loggerFactory,
        IEnumerable<IIncomingTransport> incomingTransports
    )
    {
        _bpl = bpl;
        _inputDisruptor = inputDisruptor;
        _outputDisruptor = outputDisruptor;
        _incomingTransports = incomingTransports;
        
        _inputDisruptor.SetDefaultExceptionHandler(new DisruptorExceptionHandler<IncomingDisruptorMessage>(
            exceptionHandler, loggerFactory.CreateLogger<DisruptorExceptionHandler<IncomingDisruptorMessage>>()));
        
        if (config.PersistMarketData) _outputDisruptor.HandleEventsWith(multicast).Then(persister);
        else _outputDisruptor.HandleEventsWith(multicast);
        _outputDisruptor.SetDefaultExceptionHandler(new DisruptorExceptionHandler<OutgoingDisruptorMessage>(
            exceptionHandler, loggerFactory.CreateLogger<DisruptorExceptionHandler<OutgoingDisruptorMessage>>()));
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _bpl.StartAsync(cancellationToken);
        
        _inputDisruptor.Start();
        _outputDisruptor.Start();

        await Task.WhenAll(_incomingTransports.Select(t => t.StartAsync(cancellationToken)));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _inputDisruptor.Halt();
        _outputDisruptor.Halt();
        return Task.CompletedTask;
    }
}