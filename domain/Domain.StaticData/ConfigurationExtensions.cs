using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Commands.StaticData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Domain.StaticData.QueryHandlers;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddStaticDataQueryHandlers(this IServiceCollection sc) => sc
        .AddSingleton<IQueryHandler<GetAsset, Asset?>, GetAssetQueryHandler>()
        .AddSingleton<IQueryHandler<GetAssetByExternalId, Asset?>, GetAssetByExternalIdQueryHandler>()
        .AddSingleton<IQueryHandler<GetContractByExternalId, Contract?>, GetContractByExternalIdQueryHandler>()
        .AddSingleton<IQueryHandler<GetContract, Contract?>, GetContractQueryHandler>()
        .AddSingleton<IQueryHandler<GetContracts, IReadOnlyCollection<Contract>>, GetContractsQueryHandler>()
        .AddSingleton<IQueryHandler<GetCurrency, Currency?>, GetCurrencyQueryHandler>()
        .AddSingleton<IQueryHandler<GetConversionPath, IReadOnlyCollection<FxConversionStep>>, GetFxConversionPathQueryHandler>();
    
    public static IServiceCollection AddCachingStaticDataRepository(this IServiceCollection sc) => sc
        .AddSingleton<CachingStaticDataRepository>()
        .AddSingleton<IQueryHandler<GetBroker, Broker?>>(sp => sp.GetRequiredService<CachingStaticDataRepository>())
        .AddSingleton<IQueryHandler<GetContract, Contract?>>(sp => sp.GetRequiredService<CachingStaticDataRepository>())
        .AddSingleton<IQueryHandler<GetContracts, IReadOnlyCollection<Contract>>>(sp => sp.GetRequiredService<CachingStaticDataRepository>())
        .AddSingleton<IQueryHandler<GetAsset, Asset?>>(sp => sp.GetRequiredService<CachingStaticDataRepository>())
        .AddSingleton<IQueryHandler<GetAssetByExternalId, Asset?>>(sp => sp.GetRequiredService<CachingStaticDataRepository>())
        .AddSingleton<IQueryHandler<GetContractByExternalId, Contract?>>(sp => sp.GetRequiredService<CachingStaticDataRepository>())
        .AddSingleton<IQueryHandler<GetCurrency, Currency?>>(sp => sp.GetRequiredService<CachingStaticDataRepository>())
        .AddSingleton<IQueryHandler<GetConversionPath, IReadOnlyCollection<FxConversionStep>>>(sp => sp.GetRequiredService<CachingStaticDataRepository>())
        .AddSingleton<IQueryHandler<GetContractOrderBookSubscriptionServiceName, string?>>(sp => sp.GetRequiredService<CachingStaticDataRepository>())
        .AddSingleton<ICommandHandler<ClearStaticDataCacheCmd>>(sp => sp.GetRequiredService<CachingStaticDataRepository>());
    
    public static IServiceCollection AddInMemoryStaticDataStore(this IServiceCollection sc) => sc
        .AddSingleton<InMemoryStaticDataRepositoryStateStore>()
        .AddSingleton<IStaticDataRepositoryStateStore>(sp => sp.GetRequiredService<InMemoryStaticDataRepositoryStateStore>());
}