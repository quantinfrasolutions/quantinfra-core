using NodaTime;
using QuantInfra.Sdk.MarketData;
using ExchangeBarsParquetWriter = QuantInfra.Backtesting.ParquetBarsStorage.ParquetWriter;
using ParquetHistoryProvider = QuantInfra.Backtesting.ParquetBarsStorage.ParquetFileMarketDataHistoryProvider;

namespace LocalMarketDataStorage.Tests;

public class ParquetWriterTests
{
    [Test]
    public async Task WrittenBarsCanBeReadByHistoryProvider()
    {
        var filePath = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid():N}.parquet");
        var firstOpen = Instant.FromUtc(2025, 1, 1, 12, 0);
        var bars = new[]
        {
            new ExchangeBar(100, 200, firstOpen, firstOpen.Plus(Duration.FromMinutes(1)),
                1, 2, 0.5, 1.5, 10, 15, 1),
            new ExchangeBar(100, 200, firstOpen.Plus(Duration.FromMinutes(1)),
                firstOpen.Plus(Duration.FromMinutes(2)), 1.5, 3, 1, 2.5, 20, 50, 1),
        };

        try
        {
            await ExchangeBarsParquetWriter.WriteAsync(filePath, bars, rowGroupSize: 1);

            var provider = new ParquetHistoryProvider(filePath, 100, 200);
            var result = provider.GetBAUsByStream(100, firstOpen,
                firstOpen.Plus(Duration.FromMinutes(2))).ToList();

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Select(bar => bar.OpenDt),
                    Is.EqualTo(bars.Select(bar => bar.OpenDt)));
                Assert.That(result.Select(bar => bar.Open),
                    Is.EqualTo(bars.Select(bar => bar.Open)));
                Assert.That(result.Select(bar => bar.High),
                    Is.EqualTo(bars.Select(bar => bar.High)));
                Assert.That(result.Select(bar => bar.Low),
                    Is.EqualTo(bars.Select(bar => bar.Low)));
                Assert.That(result.Select(bar => bar.Close),
                    Is.EqualTo(bars.Select(bar => bar.Close)));
                Assert.That(result.Select(bar => bar.Volume),
                    Is.EqualTo(bars.Select(bar => bar.Volume)));
            });
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
