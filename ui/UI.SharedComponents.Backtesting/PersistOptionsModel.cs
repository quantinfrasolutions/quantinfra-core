using QuantInfra.Sdk.Backtesting;

namespace UI.SharedComponents.Backtesting;

public class PersistOptionsModel
{
    public bool SaveStrategies { get; set; } = false;
    public bool SaveTrades { get; set; } = true;
    public int ExpectedTradesPerDay { get; set; } = 5;
    public bool SavePositions { get; set; } = true;
    public bool SaveDailyReturns { get; set; } = true;
    public bool DoNotSaveZeroDailyReturns { get; set; } = false;
    public bool SaveEndOfDayValues { get; set; } = true;
    public int ExpectedPositionsAtEOD { get; set; } = 5;
    public bool SaveMetrics { get; set; } = true;

    public PersistOptions ToSdk() => new()
    {
        SaveStrategies = SaveStrategies,
        SaveTrades = SaveTrades,
        ExpectedNumberOfTradesPerDay = ExpectedTradesPerDay,
        SavePositions = SavePositions,
        SaveDailyReturns = SaveDailyReturns,
        DoNotSaveZeroDailyReturns = DoNotSaveZeroDailyReturns,
        SaveEndOfDayValues = SaveEndOfDayValues,
        ExpectedNumberOfOpenPositionsAtEndOfDay = ExpectedPositionsAtEOD,
        SaveMetrics = SaveMetrics,
    };
}