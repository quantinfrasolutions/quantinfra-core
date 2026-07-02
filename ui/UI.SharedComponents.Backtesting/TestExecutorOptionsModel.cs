using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.Strategies;

namespace UI.SharedComponents.Backtesting;

public class TestExecutorOptionsModel
{
    [Required] public Instant StartDt { get; set; }
    [Required] public Instant EndDt { get; set; }
    public LogLevel LogLevel { get; set; } = LogLevel.None;
    [Required] public decimal Investment { get; set; } = 100000m;
    public Period? CandlesTimeframe { get; set; } = Period.FromMinutes(1);
    [Required] public Duration MtmUtcOffset { get; set; } = Duration.Zero;
    [Required] public int RequestBarAttempts { get; set; } = 1;
    public bool ThrowOnZeroVolumeOrders { get; set; }
    public int VirtualAccountSizeStepFraction { get; set; } = 100;
    public int DaysInYear { get; set; } = 252;
    public bool CheckPendingOrdersExecutionUsingHighLow { get; set; } = false;
    public bool CheckOrdersAtBarOpen { get; set; } = true;
    public bool CheckOrdersAtBarClose { get; set; } = false;
    public bool LimitCloseCheckToMarketOrdersOnly { get; set; } = true;
    public StopOrdersExecution StopOrdersExecution { get; set; } = StopOrdersExecution.BarClose;
    public Duration OpenExecutionOffset { get; set; } = Duration.FromSeconds(1);
    public Duration HighLowExecutionOffset { get; set; } = Duration.FromSeconds(44);

    public TestExecutorOptions ToSdk() => new()
    {
        StartDt = StartDt,
        EndDt = EndDt,
        LogLevel = LogLevel,
        Investment = Investment,
        CandlesTimeframe = CandlesTimeframe,
        MtmUtcOffset = MtmUtcOffset,
        RequestBarAttempts = RequestBarAttempts,
        ThrowOnZeroVolumeOrders = ThrowOnZeroVolumeOrders,
        VirtualAccountSizeStepFraction = VirtualAccountSizeStepFraction,
        DaysInYear = DaysInYear,
        CheckPendingOrdersExecutionUsingHighLow = CheckPendingOrdersExecutionUsingHighLow,
        CheckOrdersAtBarOpen = CheckOrdersAtBarOpen,
        CheckOrdersAtBarClose = CheckOrdersAtBarClose,
        LimitCloseCheckToMarketOrdersOnly = LimitCloseCheckToMarketOrdersOnly,
        StopOrdersExecution = StopOrdersExecution,
        OpenExecutionOffset = OpenExecutionOffset,
        HighLowExecutionOffset = HighLowExecutionOffset,
    };
}