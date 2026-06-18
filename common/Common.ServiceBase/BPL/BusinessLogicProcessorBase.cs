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
    private readonly Histogram? _receiveBarHop;
    private readonly Histogram? _processingDelay;
    private readonly Histogram? _processingTime;
    private readonly Histogram? _bplDelay;
    private readonly Histogram? _bplTime;
    private readonly Histogram? _stateTime;

    protected TState State { get; private init; }

    protected BusinessLogicProcessorBase(
        WalManager<TState> walManager,
        // DownstreamFilter filter,
        // Finalizer.Finalizer finalizer,
        TState state,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor,
        ReplayingClock clock, 
        ILogger logger,
        bool writePerformanceMetrics = false,
        bool singleHost = false
    )
    {
        _walManager = walManager;
        // _filter = filter;
        // _finalizer = finalizer;
        _outputDisruptor = outputDisruptor;
        State = state;
        _clock = clock;
        Logger = logger;
        _singleHost = singleHost;

        if (writePerformanceMetrics)
        {
            _writePerformanceMetrics = true;
            _receiveBarHop = SharedMetricsDefinition.ReceiveBarHop;
            _processingDelay = SharedMetricsDefinition.ProcessingDelay;
            _bplDelay = MetricsDefinition.BplDelay;
            _bplTime = MetricsDefinition.BplTime;
            _processingTime = SharedMetricsDefinition.ProcessingTime;
            _stateTime = MetricsDefinition.StateTime;
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
                    _receiveBarHop!.Observe((_singleHost ? data.SwReceivedAt : data.ReceivedAt) - data.TransportMessage.SendingTimestamp);
                
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