using System.Text.Json.Serialization;

namespace QuantInfra.Binance.Futures.USDM.MarketData;

public partial class BinanceMarketDataClient
{
    static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
    {
        settings.NumberHandling = JsonNumberHandling.AllowReadingFromString |
                                  JsonNumberHandling.AllowNamedFloatingPointLiterals;
        settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }
}