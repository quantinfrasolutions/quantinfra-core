using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using NodaTime;
using NodaTime.Text;

namespace QuantInfra.Backtesting.FileResultsRepository;

public sealed class InstantCsvConverter : DefaultTypeConverter
{
    public override object ConvertFromString(
        string? text,
        IReaderRow row,
        MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return default(Instant);

        return InstantPattern.ExtendedIso.Parse(text).Value;
    }

    public override string ConvertToString(
        object? value,
        IWriterRow row,
        MemberMapData memberMapData)
    {
        return value is Instant instant
            ? InstantPattern.ExtendedIso.Format(instant)
            : "";
    }
}

public sealed class LocalDateCsvConverter : DefaultTypeConverter
{
    public override object ConvertFromString(
        string? text,
        IReaderRow row,
        MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return default(LocalDate);

        return LocalDatePattern.Iso.Parse(text).Value;
    }

    public override string ConvertToString(
        object? value,
        IWriterRow row,
        MemberMapData memberMapData)
    {
        return value is LocalDate date
            ? LocalDatePattern.Iso.Format(date)
            : "";
    }
}