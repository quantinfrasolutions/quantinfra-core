using Common.Metrics;
using Microsoft.Extensions.Logging;
using NodaTime;
using Prometheus;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Metrics;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Events.Accounts.AccountsService;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.HostedStrategies;
using StrategiesCore;
using MetricsDefinition = StrategiesCore.MetricsDefinition;

namespace QuantInfra.Services.StrategiesCore;

public class Bpl : Disruptor.IEventHandler<IncomingDisruptorMessage>
{
    private readonly HostedStrategiesRunner _runner;
    private readonly IClock _clock;
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private readonly ICommandBus _commandBus;
    private readonly DisruptorAsyncQueryBus _responseBus;
    private readonly StrategiesServiceState _state;
    private readonly bool _writePerformanceMetrics;
    
    private readonly Histogram? _receiveBarHop;
    private readonly Histogram? _processingDelay;
    private readonly Histogram? _processingTime;
    private readonly Histogram? _totalMarketDataDelay;
    private readonly bool _singleHost;

    public Bpl(
        Config config,
        HostedStrategiesRunner runner,
        IClock clock, 
        ILogger<Bpl> logger, 
        IEventBus eventBus, 
        ICommandBus commandBus,
        DisruptorAsyncQueryBus responseBus,
        StrategiesServiceState state
    )
    {
        _runner = runner;
        _clock = clock;
        _logger = logger;
        _eventBus = eventBus;
        _commandBus = commandBus;
        _responseBus = responseBus;
        _state = state;
        _singleHost = config.SingleHost;
        if (config.WritePerformanceMetrics)
        {
            _writePerformanceMetrics = true;
            _receiveBarHop = SharedMetricsDefinition.ReceiveBarHop;
            _processingDelay = SharedMetricsDefinition.ProcessingDelay;
            _processingTime = SharedMetricsDefinition.ProcessingTime;
            _totalMarketDataDelay = MetricsDefinition.TotalMarketDataDelay;
        }
    }

    public void OnEvent(IncomingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        var receivedAt = data.ReceivedAt;
        var swStartProcessing = MetricsUtils.GetUnixMicro();
        long swReceivedAt = 0;
        if (data.TransportMessage is not null)
        {
            if (_writePerformanceMetrics)
            {
                swReceivedAt = data.SwReceivedAt;
                _receiveBarHop!.Observe((_singleHost ? swReceivedAt : receivedAt) - data.TransportMessage.SendingTimestamp);
                _processingDelay!.Observe(swStartProcessing - swReceivedAt);
            }
        }

        if (data.ParsedMessage != null)
        {
            Handle(data.ParsedMessage, _clock.GetCurrentInstant());

            if (_writePerformanceMetrics && data.ParsedMessage is not AccountsServiceHeartbeatEvt)
            {
                _processingTime!.Observe(MetricsUtils.GetUnixMicro() - swStartProcessing);
            }
        }
    }

    internal void Handle(object message, Instant processingDt)
    {
        switch (message)
        {
            case AccountsServiceHeartbeatEvt heartbeat:
                _state.LastProcessedEvtDt = heartbeat.Timestamp;
                break;
            case IEvent e:
                _logger.LogDebug("Processing event: eventId={eventId}, type={type}", e.EventId, e.GetType().Name);
                _eventBus.ApplyAnonymousExternalEvent(e);
                _eventBus.EmitAnonymousEvent(e);

                if (_writePerformanceMetrics && e is Candle1MClosedEvt bar)
                {
                    _totalMarketDataDelay!.Observe((processingDt - bar.Bar.CloseDt).TotalMilliseconds);
                }
                
                _state.LastProcessedEvtDt = e.Timestamp;
                break;
            case ICommand c:
                _logger.LogInformation($"Processing command: {c}");
                _commandBus.SendAnonymousCommand(c);
                break;
            case IAsyncQuery q:
                _responseBus.HandleAnonymousAsyncQuery(q);
                break;
            case AsyncQueryResponse a:
                _eventBus.HandleAnonymousAsyncQueryResult(a);
                break;
        }
    }

    
}