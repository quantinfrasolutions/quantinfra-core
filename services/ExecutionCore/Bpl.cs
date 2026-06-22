using Common.Metrics;
using Microsoft.Extensions.Logging;
using NodaTime;
using Prometheus;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Metrics;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Queries.Accounts.ExecutionService;
using QuantInfra.Sdk.Accounts.ExternalAccounts;
using QuantInfra.Sdk.Trading.Infrastructure;
using QuantInfra.Services.ExecutionCore.EventHandlers;
using QuantInfra.Services.ExecutionCore.Queries;

namespace QuantInfra.Services.ExecutionCore;

public class Bpl : Disruptor.IEventHandler<IncomingDisruptorMessage>
{
    private readonly IClock _clock;
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;
    private readonly ExternalTradingEventsHandler _handler;
    private readonly DisruptorAsyncQueryBus _responseBus;
    private readonly bool _writePerformanceMetrics;
    
    private readonly Histogram? _receiveBarHop;
    private readonly Histogram? _processingDelay;
    private readonly Histogram? _processingTime;
    private readonly bool _singleHost;

    public Bpl(
        Config config,
        IClock clock, 
        ILogger<Bpl> logger, 
        IEventBus eventBus, 
        ICommandBus commandBus,
        IQueryBus queryBus,
        ExternalTradingEventsHandler handler, 
        DisruptorAsyncQueryBus responseBus
    )
    {
        _clock = clock;
        _logger = logger;
        _eventBus = eventBus;
        _commandBus = commandBus;
        _queryBus = queryBus;
        _handler = handler;
        _responseBus = responseBus;
        _singleHost = config.SingleHost;
        if (config.WritePerformanceMetrics)
        {
            _writePerformanceMetrics = true;
            _receiveBarHop = SharedMetricsDefinition.GetIncomingMessageHop(config.ExecutionServiceName, config.SingleHost, config.Monolith,
                config.ReceiveMessageHopHistParams[0], config.ReceiveMessageHopHistParams[1], config.ReceiveMessageHopHistParams[2]);
            _processingDelay = SharedMetricsDefinition.GetProcessingDelayHistogram(config.ExecutionServiceName, config.Monolith,
                config.ProcessingDelayParams[0], config.ProcessingDelayParams[1], config.ProcessingDelayParams[2]);
            _processingTime = SharedMetricsDefinition.GetProcessingTime(config.ExecutionServiceName, config.Monolith,
                config.ProsessingTimeParams[0], config.ProsessingTimeParams[1], config.ProsessingTimeParams[2]);
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

            if (_writePerformanceMetrics)
            {
                _processingTime!.Observe(MetricsUtils.GetUnixMicro() - swStartProcessing);
            }
        }
    }

    internal void Handle(object message, Instant processingDt)
    {
        switch (message)
        {
            case IEvent e:
                _logger.LogDebug("Processing event: eventId={EventId}, type={Type}", e.EventId, e.GetType().Name);
                _eventBus.ApplyAnonymousExternalEvent(e);
                _eventBus.EmitAnonymousEvent(e);
                break;
            case ICommand c:
                _logger.LogInformation($"Processing command: {c}");
                _commandBus.SendAnonymousCommand(c);
                break;
            case GetExternalAccountSnapshot s:
                var client = _queryBus.Query<GetTradingClient, IHostedTradingClient?>(new(s.AccountId));
                if (client == null)
                {
                    _responseBus.SendAsyncQueryResponse(new AsyncQueryResponse<GetExternalAccountSnapshot, ExternalAccountFullSnapshot?>(s.RequestId, null));
                    return;
                }
                client.RequestAccountFullSnapshot(s.LastReceivedTradeDts, s.LastReceivedBalanceOperationDt, s.RequestId);
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