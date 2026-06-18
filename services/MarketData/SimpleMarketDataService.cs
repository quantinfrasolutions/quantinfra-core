using Disruptor.Dsl;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.Handlers;

namespace QuantInfra.Services.MarketData;

/// <summary>
/// Used only for constant value streams
/// </summary>
public class SimpleMarketDataService : IHostedService
{
    private readonly Bpl _bpl;
    private readonly Disruptor<IncomingDisruptorMessage> _inputDisruptor;
    private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;

    public SimpleMarketDataService(
        Config config,
        Parser parser,
        Bpl bpl,
        MulticastSender multicast,
        Disruptor<IncomingDisruptorMessage> inputDisruptor,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor,
        Persister persister,
        ILogger<SimpleMarketDataService> logger
    )
    {
        _bpl = bpl;
        _inputDisruptor = inputDisruptor;
        _outputDisruptor = outputDisruptor;
        
        if (config.Monolith) _inputDisruptor.HandleEventsWith(bpl);
        else _inputDisruptor.HandleEventsWith(parser).Then(bpl);
        _inputDisruptor.SetDefaultExceptionHandler(new FailFastExceptionHandler<IncomingDisruptorMessage>(logger));
        _outputDisruptor.HandleEventsWith(multicast).Then(persister);
        _outputDisruptor.SetDefaultExceptionHandler(new FailFastExceptionHandler<OutgoingDisruptorMessage>(logger));
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _inputDisruptor.Start();
        _outputDisruptor.Start();
        
        await _bpl.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _inputDisruptor.Halt();
        _outputDisruptor.Halt();
        return Task.CompletedTask;
    }
}