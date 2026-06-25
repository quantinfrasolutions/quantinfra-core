using NodaTime;

namespace QuantInfra.Common.Strategies.Abstractions;

public class HostedStrategiesRunnerConfig
{
    public HostedStrategiesRunnerConfig() { }

    public HostedStrategiesRunnerConfig(Duration skipProcessingOfHistoryBeforeFromNow, int requestBarAttempts, bool throwOnZeroVolumeOrders, int virtualAccountSizeStepFraction, bool writePerformanceMetrics)
    {
        SkipProcessingOfHistoryBeforeFromNow = skipProcessingOfHistoryBeforeFromNow;
        RequestBarAttempts = requestBarAttempts;
        ThrowOnZeroVolumeOrders = throwOnZeroVolumeOrders;
        VirtualAccountSizeStepFraction = virtualAccountSizeStepFraction;
        WritePerformanceMetrics = writePerformanceMetrics;
    }

    public HostedStrategiesRunnerConfig(HostedStrategiesRunnerConfig config)
    {
        SkipProcessingOfHistoryBeforeFromNow = config.SkipProcessingOfHistoryBeforeFromNow;
        RequestBarAttempts = config.RequestBarAttempts;
        ThrowOnZeroVolumeOrders = config.ThrowOnZeroVolumeOrders;
        VirtualAccountSizeStepFraction = config.VirtualAccountSizeStepFraction;
        WritePerformanceMetrics = config.WritePerformanceMetrics;
    }
    
    /// <summary>
    /// This parameter allows to set the time when the strategy becomes active (e.g., can send new orders) when processing historical data.
    /// E.g., if Strategies Service starts just a few moments after a daily bar closed, the strategy can still send (a slightly delayed) order.  
    /// </summary>
    public Duration SkipProcessingOfHistoryBeforeFromNow { get; init; } = Duration.Zero;
    
    /// <summary>
    /// When bar storages are initialized, several attempts are made to retrieve the required number
    /// of basic aggregation units from the storage, each stepping to the beginning of the previous month.
    /// This approach tries to make sure there is enough bars to initialize the storages, not knowing how many units are
    /// available in the storage. 
    /// </summary>
    public int RequestBarAttempts { get; init; } = 1;

    /// <summary>
    /// Defines if an exception should be thrown when a strategy tries to place an order with zero volume. Recommended setup is
    /// true for backtesting (to catch the orders that cannot be executed) and false for live trading.
    /// </summary>
    public bool ThrowOnZeroVolumeOrders { get; init; } = false;

    /// <summary>
    /// Defines the contract size step for virtual accounts.
    /// </summary>
    public int VirtualAccountSizeStepFraction { get; init; } = 100;
    // TODO: add option to use Contract.NormalizeVolume on virtual accounts
    
    public bool WritePerformanceMetrics { get; set; }
}