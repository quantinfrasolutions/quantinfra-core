using ConsoleAppFramework;
using NodaTime;
using NodaTime.Text;
using QuantInfra.Backtesting.TextBarsStorage;
using QuantInfra.Sdk.MarketData;
using ExchangeBarsParquetWriter = QuantInfra.Backtesting.ParquetBarsStorage.ParquetWriter;

namespace QuantInfra.Core.Apps.ConvertCsvToParquet;

internal class Commands
{
    /// <summary>
    /// Converts csv file to parquet compatible with QuantInfra Tester. Default column order is OpenDt,Open,High,Low,Close,Volume.
    /// </summary>
    /// <param name="input">-i, Input csv file path</param>
    /// <param name="streamId">-s, Stream ID</param>
    /// <param name="outputDir">-o, Path to output directory</param>
    /// <param name="tz">Tzdb name of the time zone</param>
    /// <param name="tf">Timeframe of the candles in the csv file. Use NodaTime.PeriodPatter.Roundtrip, e.g. PT1M for 1 minute, PT15M for 15 minutes, PT1H for 60 minutes</param>
    /// <param name="datetimeFormat">Default is "uuuu'-'MM'-'dd' 'HH':'mm':'ss", e.g. 2026-01-01 12:55:12</param>
    /// <param name="separator">Column separator in the csv file, default is comma</param>
    /// <param name="dtColNum">Index of the OpenDt column</param>
    /// <param name="openColNum">Index of the Open column</param>
    /// <param name="highColNum">Index of the High column</param>
    /// <param name="lowColNum">Index of the Low column</param>
    /// <param name="closeColNum">Index of the Close column</param>
    /// <param name="volColNum">Index of the Volume column</param>
    [Command("")]
    public async Task Convert(
        string input,
        int streamId,
        string? outputDir = null,
        string tz = "UTC",
        string tf = "PT1M",
        string datetimeFormat = "uuuu'-'MM'-'dd' 'HH':'mm':'ss",
        string separator = ",",
        int dtColNum = 0,
        int openColNum = 1,
        int highColNum = 2,
        int lowColNum = 3,
        int closeColNum = 4,
        int volColNum = 5,
        int rowGroupSize = ExchangeBarsParquetWriter.DefaultRowGroupSize
    )
    {
        if (separator.Length != 1)
            throw new ArgumentException("Separator must be exactly one character.", nameof(separator));

        var timezone = DateTimeZoneProviders.Tzdb[tz];
        var timeframe = PeriodPattern.Roundtrip.Parse(tf).Value;
        if (string.IsNullOrWhiteSpace(outputDir)) outputDir = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(outputDir);
        var targetFile = Path.Combine(outputDir, $"{streamId}-{PeriodPattern.Roundtrip.Format(timeframe)}.parquet");
        
        using var reader = new StreamReader(input);
        var bs = new StreamBarsStorage(reader, streamId, null, 
            LocalDateTimePattern.CreateWithInvariantCulture(datetimeFormat),
            separator[0], dtColNum, openColNum, highColNum, lowColNum, closeColNum, volColNum
        );

        var tfDur = timeframe.ToDuration();
        await ExchangeBarsParquetWriter.WriteAsync(
            targetFile,
            ReadBars(bs, streamId, tfDur, timezone),
            rowGroupSize);

        await Console.Out.WriteLineAsync(targetFile);
    }

    private static IEnumerable<ExchangeBar> ReadBars(
        StreamBarsStorage storage,
        int streamId,
        Duration timeframe,
        DateTimeZone timezone)
    {
        while (storage.CanRead)
        {
            yield return storage.Read(streamId, timeframe, timezone);
        }
    }
}
