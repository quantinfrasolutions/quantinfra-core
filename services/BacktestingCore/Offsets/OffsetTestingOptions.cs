using Common.Backtesting;

namespace BacktestingCore.Offsets;

public class OffsetTestingOptions : TestExecutorOptions
{
    public OffsetTestingOptions() { }
    
    public OffsetTestingOptions(TestExecutorOptions o) : base(o)
    { }
    
    
    public int OffsetStepMinutes { get; init; } = 1;
    public int MinOffset { get; init; } = 1;
    public int MaxOffset { get; init; } = 59;
}