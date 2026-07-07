using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Common.Backtesting.Abstractions;

public sealed record MetricsTable(IReadOnlyList<string> Columns, IReadOnlyList<MetricsRecord> Rows);

public sealed record MetricsRecord(
    Guid TestId,
    int? StrategyId,
    string? Label,
    IReadOnlyDictionary<string, double?> Columns
) : ITestMetrics;