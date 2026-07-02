using QuantInfra.Sdk.Backtesting;

namespace UI.SharedComponents.Backtesting;

public class PersistOptionsModel
{
    public bool SaveStrategies { get; set; } = false;
    public bool SaveTrades { get; set; } = true;
    public bool SavePositions { get; set; } = true;
    public bool SaveDailyReturns { get; set; } = true;
    public bool DoNotSaveZeroDailyReturns { get; set; } = false;
    public bool SaveEndOfDayValues { get; set; } = true;
    public bool SaveMetrics { get; set; } = true;

    public PersistOptions ToSdk() => new()
    {
        SaveStrategies = SaveStrategies,
        SaveTrades = SaveTrades,
        SavePositions = SavePositions,
        SaveDailyReturns = SaveDailyReturns,
        DoNotSaveZeroDailyReturns = DoNotSaveZeroDailyReturns,
        SaveEndOfDayValues = SaveEndOfDayValues,
        SaveMetrics = SaveMetrics,
    };
}