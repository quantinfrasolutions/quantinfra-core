using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Connectors.Binance.StaticDataClient;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddCachingBinanceStaticDataClient(this IServiceCollection services) => services
        .AddSingleton<BinanceStaticDataClient>()
        .AddSingleton<CachingBinanceStaticDataClient>()
        .AddSingleton<IBinanceStaticDataClient>(sp => sp.GetRequiredService<CachingBinanceStaticDataClient>());
}