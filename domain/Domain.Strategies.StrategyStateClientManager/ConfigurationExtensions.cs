using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Strategies.AccountsService;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Sdk.Strategies;
using GetStrategyState = QuantInfra.Domain.Queries.Strategies.AccountsService.GetStrategyState;

namespace QuantInfra.Domain.Strategies.StrategyStateClientManager;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddStrategiesStateClientManager(this IServiceCollection sc) => sc
        .AddSingleton<StrategiesStateManager>()
        .AddSingleton<IExternalEventHandler<StrategyInternalStateUpdatedEvt>>(sp => sp.GetRequiredService<StrategiesStateManager>())
        .AddSingleton<IExternalEventHandler<StrategyLastCalculationTsUpdatedEvt>>(sp => sp.GetRequiredService<StrategiesStateManager>())
        .AddSingleton<IAsyncQueryResponseHandler<GetStrategyState, StrategyStateReadonly?>>(sp => sp.GetRequiredService<StrategiesStateManager>())
    
        .AddSingleton<IQueryHandler<global::QuantInfra.Domain.Queries.Strategies.GetStrategyState, IStrategyStateReadonly?>>(sp => sp.GetRequiredService<StrategiesStateManager>())
        .AddSingleton<IQueryHandler<GetStrategy, IStrategy?>>(sp => sp.GetRequiredService<StrategiesStateManager>());
    
    
}