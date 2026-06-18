using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Databases.Main.DAL;

public class BinanceActiveSubscriptionsRepository(IServiceProvider sp) : IBinanceActiveSubscriptionsRepository
{
    public async Task AddSubscriptionAsync(BinanceUsdmMarketDataSubscription subscription)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        context.BinanceUsdmMarketDataSubscriptions.Add(subscription);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<BinanceUsdmMarketDataSubscriptionListView>> GetActiveSubscriptionsAsync(string clientName)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return await context.BinanceUsdmMarketDataSubscriptions
            .Where(s => s.ClientName == clientName)
            .Select(s => new BinanceUsdmMarketDataSubscriptionListView(s, 
                context.Streams.Where(st => st.StreamId == s.StreamId).SingleOrDefault()
            ))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task RemoveSubscriptionAsync(int id)
    {
        await using var scope = sp.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        var subscription = await context.BinanceUsdmMarketDataSubscriptions.FindAsync(id);
        context.BinanceUsdmMarketDataSubscriptions.Remove(subscription!);
        await context.SaveChangesAsync();
    }

    public Task UpdateSubscriptionStreamAsync(int id, long? streamId)
    {
        throw new NotImplementedException();
    }
}