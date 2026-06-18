using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Databases.Main.DAL;

public class BinanceUsdmOrderBookSubscriptionsRepository(IServiceProvider sp) : IBinanceOrderBookSubscriptionsRepository
{
    public async Task AddSubscriptionAsync(BinanceUsdmOrderBookSubscription subscription)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        context.BinanceUsdmOrderBookSubscriptions.Add(subscription);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<BinanceUsdmOrderBookSubscriptionListView>> GetActiveSubscriptionsAsync(string clientName)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.BinanceUsdmOrderBookSubscriptions
            .Where(s => s.ClientName == clientName)
            .Select(s => new BinanceUsdmOrderBookSubscriptionListView(s, 
                context.Contracts.Where(c => c.ContractId == s.ContractId).SingleOrDefault()
            ))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task RemoveSubscriptionAsync(int id)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        var subscription = await context.BinanceUsdmOrderBookSubscriptions.FindAsync(id);
        context.BinanceUsdmOrderBookSubscriptions.Remove(subscription!);
        await context.SaveChangesAsync();
    }

    public Task UpdateSubscriptionStreamAsync(int id, long? streamId)
    {
        throw new NotImplementedException();
    }
}