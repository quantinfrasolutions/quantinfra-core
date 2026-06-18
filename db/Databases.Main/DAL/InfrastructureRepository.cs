using Common.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Databases.Main.Models.Infrastructure;

namespace QuantInfra.Databases.Main.DAL;

public class InfrastructureRepository(IServiceProvider sp) : IInfrastructureRepository
{
    public async Task<IReadOnlyCollection<Location>> GetLocationsAsync()
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.Locations.ToListAsync();
    }

    public async Task<IReadOnlyCollection<AccountServiceInstance>> GetAccountServiceInstancesAsync()
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.AccountServiceInstances.ToListAsync();
    }

    public async Task<IReadOnlyCollection<StrategiesServiceInstance>> GetStrategiesServiceInstancesAsync()
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.StrategyServiceInstances.ToListAsync();
    }

    public async Task<IReadOnlyCollection<ExecutionServiceInstance>> GetExecutionServiceInstancesAsync()
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.ExecutionServiceInstances.ToListAsync();
    }

    public async Task<IReadOnlyCollection<MarketDataClientInstance>> GetMarketDataClientInstancesAsync()
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.MarketDataClients.ToListAsync();
    }

    public async Task CreateLocationAsync(Location location)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        context.Locations.Add(new LocationModel { Name = location.Name });
        await context.SaveChangesAsync();
    }

    public async Task CreateAccountServiceInstanceAsync(AccountServiceInstance instance)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        context.AccountServiceInstances.Add(new() { Name = instance.Name, LocationName = instance.LocationName });
        await context.SaveChangesAsync();
    }

    public async Task CreateStrategiesServiceInstanceAsync(StrategiesServiceInstance instance)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        context.StrategyServiceInstances.Add(new() { Name = instance.Name, LocationName = instance.LocationName });
        await context.SaveChangesAsync();
    }

    public async Task CreateExecutionServiceInstanceAsync(ExecutionServiceInstance instance)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        context.ExecutionServiceInstances.Add(new() { Name = instance.Name, LocationName = instance.LocationName });
        await context.SaveChangesAsync();
    }

    public async Task CreateMarketDataClientInstanceAsync(MarketDataClientInstance instance)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        context.MarketDataClients.Add(new() { Name = instance.Name, LocationName = instance.LocationName });
        await context.SaveChangesAsync();
    }
}