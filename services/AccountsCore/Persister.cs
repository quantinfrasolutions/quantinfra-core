using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccountsCore;
using Common.Metrics;
using Microsoft.Extensions.Logging;
using Prometheus;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Metrics;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.ServiceMessages;
using QuantInfra.Domain.Events.Accounts.AccountsService;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Accounts.AccountsService.Projections;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Persistence;
using QuantInfra.Domain.Events.Strategies.AccountsService;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Services.AccountsCore.State;

namespace QuantInfra.Services.AccountsCore;

public class Persister : Disruptor.IEventHandler<OutgoingDisruptorMessage>
{
    private readonly string _asName;
    private readonly bool _eventFlowValidationMode;
    private readonly LogLevel _logLevel;
    
    private IEventPersister? _eventPersister;
    private bool _isInitialized;
    private long _lastPersistedEventId;
    
    private List<Tuple<IEvent, IEvent?>> _events = new();
    private readonly Config _config;
    private readonly IEventPersisterFactory _eventPersisterFactory;
    private readonly ILogger<Persister> _logger;
    private readonly Counter? _persistedMessages;
    private readonly Counter? _numberOfCommits;
    private readonly Histogram? _persistTime;

    public Persister(Config config, IEventPersisterFactory eventPersisterFactory, ILogger<Persister> logger, IPersistentEventStorage<AccountServiceState> remoteStorage)
    {
        _config = config;
        _eventPersisterFactory = eventPersisterFactory;
        _logger = logger;
        _asName = config.AccountServiceName;
        _eventFlowValidationMode = config.EnableEventFlowValidation;
        _logLevel = config.LogLevel;

        if (config.WritePerformanceMetrics)
        {
            _persistedMessages = SharedMetricsDefinition.PersistedMessages;
            _numberOfCommits = SharedMetricsDefinition.NumberOfCommits;
            _persistTime = SharedMetricsDefinition.PersistTime;
        }
    }

    public SemaphoreSlim StopSemaphore { get; } = new(0, 1);

    public void OnEvent(OutgoingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        if (data.Value is StopEvt)
        {
            StopSemaphore.Release();
            return;
        }
        if (!_config.PersistEventsAndProjections) return;
        
        if (!_isInitialized) throw new InvalidOperationException("Persister not initialized");
        switch (data.Value)
        {
            case IEvent e:
                if (e is AccountsServiceHeartbeatEvt) return;
                if (e.EventId <= _lastPersistedEventId) // Replay
                {
                    // Use this to validate the sequence of events
                    // if (_eventFlowValidationMode && e.EventId != 0)
                    // {
                    //     var persistedEvent = remoteStorage.GetEvent(_asName, e.EventId);
                    //     _events.Add(Tuple.Create(e, persistedEvent));
                    //     if (e.GetType() != persistedEvent?.GetType()) Debugger.Break();
                    // }
                 
                    return;
                }
                
                // All events must be persisted in order
                if (e.EventId != _lastPersistedEventId + 1) 
                    throw new InvalidOperationException($"Persister: Expected event id {_lastPersistedEventId + 1}, got {e.EventId}");

                // Persister is always created by the next event
                EnsurePersister();
                
                HandleEvent(e);
                
                _persistedMessages?.Inc();
                
                break;
            
            case IProjectionUpdatedEvent proj:
                // During the usual processing, projections contain the same EventId as the previous event, so we
                // filter out already saved projections using the strict less.
                // However, when replaying from WAL, the last persisted event ID will be equal to the last replayed event.
                // To avoid duplication, the persister is created only by events. If the previous message with event was ignored,
                // and the persister not created, it means that the event had been persister earlier, and the projection can be ignored as well.
                if (proj.EventId < _lastPersistedEventId && _eventPersister is null) return;
                HandleProjection(proj);
                
                _persistedMessages?.Inc();
                
                break;
            
            case SyncMessage sync:
                if (_eventPersister is not null)
                {
                    var swStart = MetricsUtils.GetUnixMicro();
                    if (_logLevel <= LogLevel.Trace) _logger.LogTrace("Disposing persister");
                    _eventPersister.Dispose();
                    var elapsed = MetricsUtils.GetUnixMicro() - swStart;
                    _eventPersister = null;
                    
                    _numberOfCommits?.Inc();
                    _persistTime?.Observe(elapsed);
                }
                break;
            
            // case PersistStateEvt persistState:
            //     var persister = EnsurePersister();
            //     persister.RecordState(persistState.PartitionId, persistState.SerializedState, _lastPersistedEventId);
            //     break;
        }
    }
    
    internal async Task InitializeAsync()
    {
        if (_isInitialized) throw new InvalidOperationException("Persister already initialized");
        _lastPersistedEventId = await _eventPersisterFactory.GetLastSavedEventIdAsync(_asName);
        if (_lastPersistedEventId == 0)
        {
            _lastPersistedEventId = AccountServiceState.InitialEventId - 1;
        }
        if (_logLevel <= LogLevel.Debug) _logger.LogDebug("Persister initialized, lastPersistedEventId: {lastPersistedEventId}", _lastPersistedEventId);
        _isInitialized = true;
    }
    
    public long LastPersistedEventId => _lastPersistedEventId;

    private IEventPersister EnsurePersister()
    {
        if (_eventPersister == null)
        {
            if (_logLevel <= LogLevel.Trace) _logger.LogTrace("Creating persister");
            _eventPersister = _eventPersisterFactory.Create();
        }
        return _eventPersister;
    }

    private void HandleEvent(IEvent e)
    {
        if (_logLevel <= LogLevel.Trace) _logger.LogTrace("HandleEvent: {event}", e);
        _lastPersistedEventId = e.EventId;
        switch (e)
        {
            case AccountCreatedEvt ac:
                _eventPersister!.RecordEvent(_asName, ac);
                _lastPersistedEventId = e.EventId;
                break;
            case SubaccountAssignedEvt sa:
                _eventPersister!.RecordEvent(_asName, sa);
                _lastPersistedEventId = e.EventId;
                break;
            case AccountEndOfDayEvt eod:
                _eventPersister!.RecordEvent(_asName, eod);
                _lastPersistedEventId = e.EventId;
                break;
            case BalanceOperationProcessedEvt bo:
                _eventPersister!.RecordEvent(_asName, bo);
                _lastPersistedEventId = e.EventId;
                break;
            case ExecutionReportEvt er:
                _eventPersister!.RecordEvent(_asName, er);
                _lastPersistedEventId = e.EventId;
                break;
            case ExternalExecutionReportEvt extEr:
                _eventPersister!.RecordEvent(_asName, extEr);
                _lastPersistedEventId = e.EventId;
                break;
            case NewOrderSingleExternalCreatedEvt nos:
                _eventPersister!.RecordEvent(_asName, nos);
                _lastPersistedEventId = e.EventId;
                break;
            case NewTradeInDeadLetterQueueEvt tdlq:
                _eventPersister!.RecordEvent(_asName, tdlq);
                _lastPersistedEventId = e.EventId;
                break;
            case NewUnmappedContractRegisteredEvt un:
                _eventPersister!.RecordEvent(_asName, un);
                _lastPersistedEventId = e.EventId;
                break;
            case OrderCancelRejectEvt ocr:
                _eventPersister!.RecordEvent(_asName, ocr);
                _lastPersistedEventId = e.EventId;
                break;
            case OrderCancelRequestExternalCreatedEvt ocrExt:
                _eventPersister!.RecordEvent(_asName, ocrExt);
                _lastPersistedEventId = e.EventId;
                break;
            case OrderReplaceRequestExternalCreatedEvt ocrrExt:
                _eventPersister!.RecordEvent(_asName, ocrrExt);
                _lastPersistedEventId = e.EventId;
                break;
            case ShareCountUpdatedEvt scu:
                _eventPersister!.RecordEvent(_asName, scu);
                _lastPersistedEventId = e.EventId;
                break;
            case SharePriceUpdatedEvt spu:
                _eventPersister!.RecordEvent(_asName, spu);
                _lastPersistedEventId = e.EventId;
                break;
            case TradeEvt trade:
                _eventPersister!.RecordEvent(_asName, trade);
                _lastPersistedEventId = e.EventId;
                break;
            case AccountReconciliationStatusChangedEvt recon:
                _eventPersister!.RecordEvent(_asName, recon);
                _lastPersistedEventId = e.EventId;
                break;
            case BrokerAccountNeedsOrdersReconciliationEvt ordersRec:
                _eventPersister!.RecordEvent(_asName, ordersRec);
                break;
            case BrokerAccountOrdersReconciledEvt ordersDone:
                _eventPersister!.RecordEvent(_asName, ordersDone);
                break;
            case BrokerAccountNeedsTradesReconciliationEvt fullRec:
                _eventPersister!.RecordEvent(_asName, fullRec);
                break;
            case BrokerAccountTradesReconciledEvt tradesDone:
                _eventPersister!.RecordEvent(_asName, tradesDone);
                break;
            case TradingClientConfigurationChangedEvt tc:
                _eventPersister!.RecordEvent(_asName, tc);
                _lastPersistedEventId = e.EventId;
                break;
            
            
            case StrategyCreatedEvt strat:
                _eventPersister!.RecordEvent(_asName, strat);
                _lastPersistedEventId = e.EventId;
                break;
            case StrategyLastCalculationTsUpdatedEvt calc:
                _eventPersister!.RecordEvent(_asName, calc);
                _lastPersistedEventId = e.EventId;
                break;
            case StrategyInternalStateUpdatedEvt su:
                _eventPersister!.RecordEvent(_asName, su);
                _lastPersistedEventId = e.EventId;
                break;
            
            default: throw new NotSupportedException($"Event type {e.GetType().Name} is not supported for persistence");
        }
    }
    
    private void HandleProjection(IProjectionUpdatedEvent p)
    {
        if (_logLevel <= LogLevel.Trace) _logger.LogTrace("HandleProjection: {projection}", p);
        switch (p)
        {
            case BalanceHistoryProjectionEvt bh:
                _eventPersister?.RecordProjection(_asName, bh);
                break;
            case PositionChangedEvt pc:
                _eventPersister?.RecordProjection(_asName, pc);
                break;
            case SharePriceHistoryProjectionEvt sp:
                _eventPersister?.RecordProjection(_asName, sp);
                break;
        }
    }
}