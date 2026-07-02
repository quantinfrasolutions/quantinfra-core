using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using NodaTime.Text;

namespace QuantInfra.Databases.Backtesting.Sqlite;

internal sealed class PeriodConverter() : ValueConverter<Period, string>(
    v => PeriodPattern.Roundtrip.Format(v),
    v => PeriodPattern.Roundtrip.Parse(v).Value
);

internal sealed class DurationConverter() : ValueConverter<Duration, string>(
    v => DurationPattern.Roundtrip.Format(v),
    v => DurationPattern.Roundtrip.Parse(v).Value
);