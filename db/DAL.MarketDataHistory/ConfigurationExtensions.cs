using System;
using Common.MarketData;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Common.MarketData.Infrastructure;

namespace DAL.MarketDataHistory.Configuration
{
	public static class ConfigurationExtensions
	{
        public static IServiceCollection UseMarketDataPersisterDAL(
            this IServiceCollection services
        ) => services.AddSingleton<IMarketDataPersister, MarketDataPersister>();

        public static IServiceCollection UseMarketDataHistoryProviderDAL(
            this IServiceCollection services
        ) => services.AddSingleton<IMarketDataHistoryProvider, MarketDataHistoryProvider>();
    }
}

