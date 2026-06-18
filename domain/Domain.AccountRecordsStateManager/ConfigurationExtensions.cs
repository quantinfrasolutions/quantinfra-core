using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Domain.AccountRecordsStateManager;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddAccountRecordsClientStateManager(this IServiceCollection sc) => sc
        .AddSingleton<StateManager>()
        .AddSingleton<IExternalEventHandler<AccountCreatedEvt>>(sp => sp.GetRequiredService<StateManager>())
        .AddSingleton<IExternalEventHandler<SubaccountAssignedEvt>>(sp => sp.GetRequiredService<StateManager>())
        .AddSingleton<IExternalEventHandler<TradingClientConfigurationChangedEvt>>(sp => sp.GetRequiredService<StateManager>())
        .AddSingleton<IQueryHandler<GetAccount, AccountRecordV6?>>(sp => sp.GetRequiredService<StateManager>())
        .AddSingleton<IQueryHandler<GetBrokerAccountForSsa, int?>>(sp => sp.GetRequiredService<StateManager>())
        .AddSingleton<IQueryHandler<GetSsaIdsForBrokerAccount, IReadOnlyCollection<int>>>(sp => sp.GetRequiredService<StateManager>());
    
    public static IServiceCollection UseAccountRecordsDefaultStore(this IServiceCollection sc) => sc
        .AddSingleton<IAccountRecordsStore, DefaultStore>();
}