using Binance.Futures.USDM;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuantInfra.Services.MarketData;

namespace QuantInfra.Connectors.Binance.Futures.Usdm;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureBinanceUsdmFuturesMarketDataGateway(this IServiceCollection sc,
        IConfiguration configuration,
        string sectionName = "client",
        Action<MarketDataClientConfig>? configureAction = null
    ) => sc
        .Configure<MarketDataClientConfig>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<MarketDataClientConfig>(sc =>
        {
            var config = sc.GetRequiredService<IOptions<MarketDataClientConfig>>().Value;
            configureAction?.Invoke(config);
            return config;
        });
    
    public static IServiceCollection AddBinanceUsdmFuturesMarketDataGatewayService(this IServiceCollection sc, bool runClient = true)
    {
        sc
            .AddSingleton<MarketDataClient>()
            .AddSingleton<IMarketDataSnapshotsProvider>(sp => sp.GetRequiredService<MarketDataClient>());
        if (runClient) sc.AddHostedService(sp => sp.GetService<MarketDataClient>()!);
        return sc;
    }
}