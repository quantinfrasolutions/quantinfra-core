using Parquet.Schema;

namespace QuantInfra.Backtesting.ParquetBarsStorage;

public static class SchemaDefinition
{
    public static ParquetSchema Schema = new(
        new DataField<long>("OpenTimestamp"),
        new DataField<double>("Open"),
        new DataField<double>("High"),
        new DataField<double>("Low"),
        new DataField<double>("Close"),
        new DataField<double>("Volume")
    );
}