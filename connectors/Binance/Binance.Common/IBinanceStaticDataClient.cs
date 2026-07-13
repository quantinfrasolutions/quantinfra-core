using QuantInfra.Connectors.Binance.StaticDataClient.Models;

namespace QuantInfra.Connectors.Binance.Common;

public interface IBinanceStaticDataClient
{
    Task<IReadOnlyList<BinanceAsset>> GetAssetsAsync(BinanceMarket market);
    Task<IReadOnlyList<BinanceContract>> GetContractsAsync(BinanceMarket market);
}
