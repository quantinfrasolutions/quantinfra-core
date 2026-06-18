using Common.StaticData.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Sdk.StaticData;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Databases.Main.DAL;

public class MarketDataServiceStreamsRepository(IServiceProvider serviceProvider) : IMarketDataServiceStreamsRepository
{
    public async Task<IReadOnlyCollection<Stream>> GetEnabledStreamsAsync(string? serviceName)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        
        // HACK: there must be one MDS with empty name, serving constant streams
        // All other streams must be related to a subscription
        var res = await context.Streams
            .Where(s => 
                // s.Enabled && 
                (
                (serviceName == "MDS" && s.ConstantStreamValue != null) // TODO: HACK 
                || (
                    !string.IsNullOrEmpty(serviceName) &&
                        context.BinanceUsdmMarketDataSubscriptions.Where(b => 
                            b.StreamId == s.StreamId && b.ClientName == serviceName
                        ).Any()
                )
                || (
                    !string.IsNullOrEmpty(serviceName) &&
                        context.IbkrMarketDataSubscriptions.Where(b => 
                            b.StreamId == s.StreamId && b.ClientName == serviceName
                        ).Any()
                )
            ))
            .Include(c => c.ConstantStreamValue)
            .Include(s => s.Contract)
                .ThenInclude(c => c!.Template)
            .Include(s => s.Contract)
                .ThenInclude(c => c!.Template)
                    .ThenInclude(t => t.TradingSessions)
                        .ThenInclude(ts => ts.Exchange)
            .Include(s => s.Contract)
                .ThenInclude(c => c!.Template)
                    .ThenInclude(t => t.TradingSessions)
                        .ThenInclude(ts => ts.Days)
            .AsNoTracking()
            .ToListAsync();

        return res;
    }
}