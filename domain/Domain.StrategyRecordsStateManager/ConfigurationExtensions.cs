using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Domain.StrategyRecordsStateManager.ExternalEventHandlers;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.StrategyRecordsStateManager;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddStrategyRecordsClientStateManagerForStrategiesService(
        this IServiceCollection sc) => sc
        .AddSingleton<StrategiesServiceStateManager>()
        .AddSingleton<StrategyUpdatesHandler>()
        .AddSingleton<IExternalEventHandler<StrategyCreatedEvt>>(sp => sp.GetRequiredService<StrategyUpdatesHandler>())
        .AddSingleton<IExternalEventHandler<StrategyStatusChangedEvt>>(sp =>
            sp.GetRequiredService<StrategyUpdatesHandler>())
        .AddSingleton<IQueryHandler<GetStrategyRecord, Strategy?>>(sp => sp.GetRequiredService<StrategiesServiceStateManager>())
        .AddSingleton<IQueryHandler<GetAccount, AccountRecordV6?>>(sp => sp.GetRequiredService<StrategiesServiceStateManager>())
        // .AddSingleton<IQueryHandler<GetSubscribedEsas, IReadOnlyCollection<int>>>(sp => sp.GetRequiredService<StrategiesServiceStateManager>())
    ;
        
    public static IServiceCollection AddStrategyRecordsClientStateManagerForAccountsService(this IServiceCollection sc) => sc
        .AddSingleton<AccountsServiceStateManager>()
        .AddSingleton<StrategyUpdatesHandlerAccountsService>()
        .AddSingleton<IExternalEventHandler<StrategyCreatedEvt>>(sp => sp.GetRequiredService<StrategyUpdatesHandlerAccountsService>())
        .AddSingleton<IExternalEventHandler<StrategyStatusChangedEvt>>(sp => sp.GetRequiredService<StrategyUpdatesHandlerAccountsService>())
        .AddSingleton<IQueryHandler<GetStrategyRecord, Strategy?>>(sp => sp.GetRequiredService<AccountsServiceStateManager>());
}