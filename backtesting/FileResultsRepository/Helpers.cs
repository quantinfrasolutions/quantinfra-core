using System.Globalization;
using CsvHelper.Configuration;

namespace QuantInfra.Backtesting.FileResultsRepository;

internal static class Helpers
{
    public const string StrategiesFile = "strategies.csv";
    public const string ReturnsFile = "returns.csv";
    public const string PositionsFile = "positions.csv";
    public const string EndOfDayBalancesFile = "eod-balances.csv";
    public const string TradesFile = "trades.csv";
    public const string EndOfDayPositionsFile = "eod-positions.csv";
    public const string EndOfDayPositionValuesFile = "eod-position-values.csv";
    
    public static string GetPath(string root, Guid unitId, string fileName)
    {
        var dir = Path.Combine(root, unitId.ToString("D"));
        return Path.Combine(dir, fileName);
    }
    
    public static CsvConfiguration CreateCsvConfig()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null,
            IgnoreBlankLines = true,
        };
    }
}