using System.Globalization;
using System.Reflection;
using System.Text.Json;
using CsvHelper;
using NodaTime;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Backtesting.FileResultsRepository;

public class TestResultsFileWriter(Config config) : ITestResultsPersister
{
    public void PersistResult(TestUnit unit, StrategyTestResult result)
    {
        var directory = Path.Combine(config.WorkingDirectory, unit.TestId.ToString());
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        
        if (unit.PersistOptions.SaveStrategies)
        {
            using var file = File.Open(Path.Combine(directory, Helpers.StrategiesFile), FileMode.Create);
            JsonSerializer.Serialize(file, result.StrategyConfigs, JsonOptions.JsonSerializerOptions);
            file.Close();
        }

        if (unit.PersistOptions.SaveTrades)
        {
            var path = Path.Combine(directory, Helpers.TradesFile);
            using var writer = new StreamWriter(path);
            Write(writer, result.Trades);
        }
        
        if (unit.PersistOptions.SavePositions)
        {
            var path = Path.Combine(directory, Helpers.PositionsFile);
            using var writer = new StreamWriter(path);
            Write(writer, result.PositionCloses);
        }
        
        if (unit.PersistOptions.SaveDailyReturns)
        {
            var path = Path.Combine(directory, Helpers.ReturnsFile);
            using var writer = new StreamWriter(path);
            // TODO: skip zero returns
            Write(writer, result.Returns);
        }
        
        if (unit.PersistOptions.SaveEndOfDayValues)
        {
            var path = Path.Combine(directory, Helpers.EndOfDayBalancesFile);
            using (var writer = new StreamWriter(path)) Write(writer, result.EndOfDayBalances);
            
            path = Path.Combine(directory, Helpers.EndOfDayPositionsFile);
            using (var writer = new StreamWriter(path)) Write(writer, result.EndOfDayPositions);
            
            path = Path.Combine(directory, Helpers.EndOfDayPositionValuesFile);
            using (var writer = new StreamWriter(path)) Write(writer, result.PositionValues);
        }
    }

    public void PersistMetrics(TestUnit unit, ITestMetrics metrics)
    {
        if (unit.PersistOptions.SaveMetrics)
        {
            var directory = Path.Combine(config.WorkingDirectory, unit.TestId.ToString());
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, Helpers.MetricsFile);
            
            var runtimeType = metrics.GetType();

            var props = runtimeType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead)
                .ToArray();

            var fileExists = File.Exists(path);
            using var writer = new StreamWriter(path, append: true);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            if (!fileExists)
            {
                foreach (var prop in props)
                    csv.WriteField(prop.Name);

                csv.NextRecord();
            }

            foreach (var prop in props)
                csv.WriteField(prop.GetValue(metrics));

            csv.NextRecord();
        }
    }

    private void Write<T>(StreamWriter writer, IEnumerable<T> data)
    {
        using var csv = new CsvWriter(writer, Helpers.CreateCsvConfig());

        csv.Context.TypeConverterCache.AddConverter<Instant>(new InstantCsvConverter());
        csv.Context.TypeConverterCache.AddConverter<LocalDate>(new LocalDateCsvConverter());
        csv.WriteRecords(data);
        writer.Flush();
    }
}