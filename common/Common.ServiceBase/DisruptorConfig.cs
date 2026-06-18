using Disruptor;

namespace QuantInfra.Common.ServiceBase;

public class DisruptorConfig
{
    public int InputDisruptorRingBufferSize { get; set; } = 1024;
    public int OutputDisruptorRingBufferSize { get; set; } = 1024;
    public WaitStrategy WaitStrategy { get; set; } = WaitStrategy.BlockingWait;

    public IWaitStrategy GetWaitStrategy() => GetWaitStrategy(WaitStrategy);
    
    public static IWaitStrategy GetWaitStrategy(WaitStrategy waitStrategy) => waitStrategy switch
    {
        WaitStrategy.BlockingWait => new BlockingWaitStrategy(),
        WaitStrategy.BusySpin => new BusySpinWaitStrategy(),
        WaitStrategy.YieldingWait => new YieldingWaitStrategy(),
        WaitStrategy.SleepingWait => new SleepingWaitStrategy(),
        _ => throw new ArgumentOutOfRangeException()
    };
}

public enum WaitStrategy
{
    BlockingWait,
    BusySpin,
    YieldingWait,
    SleepingWait,
}