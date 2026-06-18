using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Connectors.Ibkr.Interfaces;

namespace QuantInfra.Databases.Main.DAL;

public class IbkrActiveSubscriptionsRepository(IServiceProvider sp) : IIbkrActiveSubscriptionsRepository
{
    public async Task AddSubscriptionAsync(IbkrMarketDataSubscription subscription)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        context.IbkrMarketDataSubscriptions.Add(subscription);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<IbkrMarketDataSubscriptionListView>> GetActiveSubscriptionsAsync(
        string clientName)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.IbkrMarketDataSubscriptions
            .Select(s => new IbkrMarketDataSubscriptionListView(s, 
                context.Streams.Where(st => st.StreamId == s.StreamId).SingleOrDefault()
            ))
            .ToListAsync();
    }

    public async Task RemoveSubscriptionAsync(int id)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        var subscription = await context.IbkrMarketDataSubscriptions.FindAsync(id);
        context.IbkrMarketDataSubscriptions.Remove(subscription!);
        await context.SaveChangesAsync();
    }

    public Task UpdateSubscriptionStreamAsync(int id, long? streamId)
    {
        throw new NotImplementedException();
    }
}