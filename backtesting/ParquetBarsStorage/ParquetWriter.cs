using Parquet.Data;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Backtesting.ParquetBarsStorage;

/// <summary>
/// Writes exchange bars in the format consumed by <see cref="ParquetFileMarketDataHistoryProvider"/>.
/// </summary>
public static class ParquetWriter
{
    public const int DefaultRowGroupSize = 100_000;

    public static void Write(
        string filePath,
        IEnumerable<ExchangeBar> bars,
        int rowGroupSize = DefaultRowGroupSize) =>
        WriteAsync(filePath, bars, rowGroupSize).GetAwaiter().GetResult();

    public static Task WriteAsync(
        string filePath,
        IEnumerable<ExchangeBar> bars,
        CancellationToken cancellationToken = default) =>
        WriteAsync(filePath, bars, DefaultRowGroupSize, cancellationToken);

    public static async Task WriteAsync(
        string filePath,
        IEnumerable<ExchangeBar> bars,
        int rowGroupSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(bars);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rowGroupSize);

        var fields = SchemaDefinition.Schema.GetDataFields();

        await using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);
        using var writer = await global::Parquet.ParquetWriter.CreateAsync(
            SchemaDefinition.Schema,
            stream,
            cancellationToken: cancellationToken);

        var batch = new List<ExchangeBar>(rowGroupSize);
        foreach (var bar in bars)
        {
            cancellationToken.ThrowIfCancellationRequested();
            batch.Add(bar);
            if (batch.Count < rowGroupSize) continue;

            await WriteRowGroupAsync(writer, fields, batch, cancellationToken);
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            await WriteRowGroupAsync(writer, fields, batch, cancellationToken);
        }
    }

    private static async Task WriteRowGroupAsync(
        global::Parquet.ParquetWriter writer,
        Parquet.Schema.DataField[] fields,
        IReadOnlyCollection<ExchangeBar> bars,
        CancellationToken cancellationToken)
    {
        using var rowGroup = writer.CreateRowGroup();
        var data = bars as List<ExchangeBar> ?? bars.ToList();

        await rowGroup.WriteColumnAsync(
            new DataColumn(fields[0], data.Select(bar => bar.OpenDt.ToUnixTimeSeconds()).ToArray()),
            cancellationToken);
        await rowGroup.WriteColumnAsync(
            new DataColumn(fields[1], data.Select(bar => bar.Open).ToArray()),
            cancellationToken);
        await rowGroup.WriteColumnAsync(
            new DataColumn(fields[2], data.Select(bar => bar.High).ToArray()),
            cancellationToken);
        await rowGroup.WriteColumnAsync(
            new DataColumn(fields[3], data.Select(bar => bar.Low).ToArray()),
            cancellationToken);
        await rowGroup.WriteColumnAsync(
            new DataColumn(fields[4], data.Select(bar => bar.Close).ToArray()),
            cancellationToken);
        await rowGroup.WriteColumnAsync(
            new DataColumn(fields[5], data.Select(bar => bar.Volume).ToArray()),
            cancellationToken);
    }
}
