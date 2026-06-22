using Common.Metrics;
using Disruptor;
using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using Prometheus;
// using QuanInfra.Common.ServiceBase.Finalizer;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Metrics;
using QuantInfra.Common.ServiceBase.Handlers;
using QuantInfra.Common.ServiceBase.ServiceMessages;
using QuantInfra.Common.ServiceBase.WAL;

namespace QuantInfra.Common.ServiceBase.BPL;

public abstract class BusinessLogicProcessorBase<TState> : IEventHandler<IncomingDisruptorMessage>
    where TState : class, IState<TState>, new()
{
    private readonly WalManager<TState> _walManager;
    // private readonly DownstreamFilter _filter;
    // private readonly QuanInfra.Common.ServiceBase.Finalizer.Finalizer _finalizer;
    private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;
    private readonly ReplayingClock _clock;
    protected readonly ILogger Logger;
    private readonly bool _singleHost;

    private long _currentWalPartition = 0;
    protected long CurrentWalPartition => _currentWalPartition;
    
    private readonly bool _writePerformanceMetrics;
    private readonly Histogram? _receiveMessageHop;
    private readonly Histogram? _processingDelay;
    private readonly Histogram? _processingTime;
    private readonly Histogram? _bplDelay;
    private readonly Histogram? _bplTime;
    private readonly Histogram? _stateTime;

    protected TState State { get; private init; }

    protected BusinessLogicProcessorBase(
        BplConfig config,
        WalManager<TState> walManager,
        TState state,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor,
        ReplayingClock clock, 
        ILogger logger
    )
    {
        _walManager = walManager;
        // _filter = filter;
        // _finalizer = finalizer;
        _outputDisruptor = outputDisruptor;
        State = state;
        _clock = clock;
        Logger = logger;
        _singleHost = config.SingleHost;

        if (config.WritePerformanceMetrics)
        {
            _writePerformanceMetrics = true;
            _receiveMessageHop = SharedMetricsDefinition.GetIncomingMessageHop(config.ServiceName, config.SingleHost, config.Monolith,
                config.ReceiveMessageHopHistParams[0], config.ReceiveMessageHopHistParams[1], config.ReceiveMessageHopHistParams[2]);
            _processingDelay = SharedMetricsDefinition.GetProcessingDelayHistogram(config.ServiceName, config.Monolith,
                config.ProcessingDelayParams[0], config.ProcessingDelayParams[1], config.ProcessingDelayParams[2]);
            _bplDelay = MetricsDefinition.GetBplDelay(config.ServiceName, config.Monolith,
                config.BplDelayParams[0], config.BplDelayParams[1], config.BplDelayParams[2]);
            _bplTime = MetricsDefinition.GetBplTime(config.ServiceName, config.Monolith,
                config.BplTimeParams[0], config.BplTimeParams[1], config.BplTimeParams[2]);
            _processingTime = SharedMetricsDefinition.GetProcessingTime(config.ServiceName, config.Monolith,
                config.ProcessingTimeParams[0], config.ProcessingTimeParams[1], config.ProcessingTimeParams[2]);
            _stateTime = MetricsDefinition.GetStateTime(config.ServiceName, config.Monolith,
                config.StateTimeParams[0], config.StateTimeParams[1], config.StateTimeParams[2]);
        }
    }
    
    public void OnEvent(IncomingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        if (data.Skip) return;
        
        if (data.IsReplay) _clock.SetCurrentInstant(data.ReceivedAt);

        var swStartProcessing = MetricsUtils.GetUnixMicro();

        if (data.TransportMessage != null)
        {
            OnBeforeHandleMessage(data.TransportMessage);
            if (_writePerformanceMetrics && !data.IsReplay)
            {
                if (data.TransportMessage.SendingTimestamp != 0)
                    _receiveMessageHop!.Observe((_singleHost ? data.SwReceivedAt : data.ReceivedAt) - data.TransportMessage.SendingTimestamp);
                
                _processingDelay!.Observe(swStartProcessing - data.SwReceivedAt);
            }
        }

        if (data.ParsedMessage != null)
        {
            if (data.ParsedMessage is StateReadFromFileEvt)
            {
                Logger.LogInformation($"Using partition {data.WalPartition}");
                _currentWalPartition = data.WalPartition;

                // _filter.UpdateLastSentEventId(State.LastFinalizedEventId);
                // _finalizer.UpdateLastSentEventId(State.LastFinalizedEventId, State.LastFinalizedTimestamp);

                OnBeforeReplayingWal();
            }
            else if (data.ParsedMessage is WalReadCompletedEvt)
            {
                Logger.LogInformation($"Replay completed");
                _clock.FinishReplay();
                OnStateInitialized();
                _outputDisruptor.PublishMessage(data.ParsedMessage);
            }
            else if (data.ParsedMessage is ReconciliationDoneEvt)
            {
                Logger.LogInformation($"Reconciliation done");
                OnReconciliationDone();
            }
            else
            {
                if (data.WalPartition < _currentWalPartition)
                    throw new Exception(
                        $"Received an event with partition {data.WalPartition}, current partition is {_currentWalPartition}");

                // if (data.ParsedMessage is FinalizeEvt fin)
                // {
                //     if (data.IsReplay)
                //     {
                //         // _filter.UpdateLastSentEventId(fin.EventId);
                //         // _finalizer.UpdateLastSentEventId(fin.EventId, fin.TimeStamp);
                //         // State.UpdateLastSentEventId(fin.EventId, fin.TimeStamp);
                //     }
                //     else
                //     {
                //         _outputDisruptor.PublishMessage(data.ParsedMessage);
                //     }
                // }

                var handleStart = MetricsUtils.GetUnixMicro();
                if (data.ParsedMessage != null)
                {
                    HandleMessage(data.ParsedMessage, data.IsReplay, _clock.GetCurrentInstant(), data.SwReceivedAt);
                }
                var handleFinish = MetricsUtils.GetUnixMicro();

                if (_writePerformanceMetrics && !data.IsReplay)
                {
                    _bplDelay!.Observe(handleStart - data.SwReceivedAt);
                    _bplTime!.Observe(handleFinish - handleStart);
                    _processingTime!.Observe(handleFinish - swStartProcessing);
                }
            }
        }
        
        if (data.WalPartition > _currentWalPartition)
        {
            var start = MetricsUtils.GetUnixMicro();
            var serialized = _walManager.PersistState(State, data.WalPartition);
            _currentWalPartition = data.WalPartition;

            if (_writePerformanceMetrics)
            {
                _stateTime!.Observe(MetricsUtils.GetUnixMicro() - start);
            }
            
            _outputDisruptor.PublishMessage(new PersistStateEvt(serialized, data.WalPartition));
        }
        
        if (data.ParsedMessage is StopEvt) _outputDisruptor.PublishMessage(data.ParsedMessage);
    }
    
    protected abstract void HandleMessage(object message, bool dataReplay, Instant processingDt,
        long dataSwReceivedAt);

    protected virtual void OnBeforeReplayingWal() { }
    protected virtual void OnStateInitialized() { }
    protected virtual void OnReconciliationDone() { }
    protected virtual void OnBeforeHandleMessage(ITransportMessage msg) { }
}