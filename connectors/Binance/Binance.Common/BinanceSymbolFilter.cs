namespace QuantInfra.Connectors.Binance.StaticDataClient.Models;

/// <summary>
/// A Binance symbol filter. Values are retained as strings so newly introduced
/// filters can be consumed without requiring a connector release.
/// </summary>
public sealed record BinanceSymbolFilter(
    string FilterType,
    IReadOnlyDictionary<string, string> Values)
{
    public decimal? GetDecimal(string name) =>
        Values.TryGetValue(name, out var value) &&
        decimal.TryParse(value, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
}
