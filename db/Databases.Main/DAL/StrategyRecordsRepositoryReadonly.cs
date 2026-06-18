using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Databases.Main.DAL;

public class StrategyRecordsRepositoryReadonly(IServiceProvider serviceProvider) : IStrategyRecordsRepositoryReadonly
{
    public async Task<IReadOnlyCollection<Strategy>> GetStrategyRecordsByStrategiesServiceNameAsync(string strategiesServiceName)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.GetStrategyRecordsByStrategiesServiceNameAsync(strategiesServiceName);
    }

    public async Task<IReadOnlyCollection<Strategy>> GetStrategyRecordsByAccountServiceName(string accountsServiceName)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.GetStrategyRecordsByAccountServiceName(accountsServiceName);
    }

    // public async Task<IReadOnlyCollection<EsaSubscription>> GetExecutableSubaccountsByStrategiesServiceNameAsync(string strategiesServiceName)
    // {
    //     await using var scope = serviceProvider.CreateAsyncScope();
    //     await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
    //     return await context.GetExecutableSubaccountsByStrategiesServiceNameAsync(strategiesServiceName);
    // }
}