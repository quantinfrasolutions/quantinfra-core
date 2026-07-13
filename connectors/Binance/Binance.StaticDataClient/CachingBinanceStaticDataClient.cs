using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Connectors.Binance.StaticDataClient.Models;

namespace QuantInfra.Connectors.Binance.StaticDataClient;

public class CachingBinanceStaticDataClient(BinanceStaticDataClient client) : IBinanceStaticDataClient
{
    private Dictionary<BinanceMarket, IReadOnlyList<BinanceAsset>> _assets = new();
    private Dictionary<BinanceMarket, IReadOnlyList<BinanceContract>> _contracts = new();
    
    public async Task<IReadOnlyList<BinanceAsset>> GetAssetsAsync(BinanceMarket market)
    {
        if (!_assets.TryGetValue(market, out var assets))
        {
            assets = await client.GetAssetsAsync(market, CancellationToken.None);
            _assets.Add(market, assets);
        }
        
        return assets;
    }

    public async Task<IReadOnlyList<BinanceContract>> GetContractsAsync(BinanceMarket market)
    {
        if (!_contracts.TryGetValue(market, out var contracts))
        {
            contracts = await client.GetContractsAsync(market, CancellationToken.None);
            _contracts.Add(market, contracts);
        }
        
        return contracts;
    }
}