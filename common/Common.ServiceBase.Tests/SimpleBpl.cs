using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.BPL;
using QuantInfra.Common.ServiceBase.WAL;

namespace Common.ServiceBase.Tests;

class SimpleBpl : BusinessLogicProcessorBase<MockState>
{
    private readonly WalTests _test;

    public SimpleBpl(
        WalTests test,
        WalManager<MockState> walManager,
        // DownstreamFilter filter,
        MockState state,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor,
        ReplayingClock clock,
        ILogger<SimpleBpl> logger
    ) : base(new(), walManager, state, outputDisruptor, clock, logger)
    {
        _test = test;
    }

    protected override void OnBeforeReplayingWal()
    {
        _test.OnBeforeReplayingWalSemaphore?.Release();
    }

    protected override void OnStateInitialized()
    {
        _test.OnStateInitializedSemaphore?.Release();
    }

    protected override void HandleMessage(object message, bool dataReplay, Instant processingDt,
        long dataSwReceivedAt)
    {
        if (message is string s)
        {
            State.LastEventId = s;
            State.EventIds.Add(s);
            
            if (int.TryParse(s, out var seq))
                _test.Semaphores.GetValueOrDefault(seq)?.Release();
        }
    }
}