using Databases.MarketDataHistory;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using QuantInfra.Common.MarketData.Infrastructure;
using QuantInfra.Sdk.MarketData;

namespace DAL.MarketDataHistory
{
    public class MarketDataPersister(IServiceProvider sp) :
        IMarketDataPersister
    {
        public void AppendBAU(ExchangeBar bau, BarAggregationType aggType)
        {
            using var context = sp.CreateScope().ServiceProvider.GetRequiredService<MDTimescaleContext>();
            context.AppendBAU(bau, aggType);
        }

        public async Task AppendBAUAsync(ExchangeBar bar, BarAggregationType aggType)
        {
            throw new NotImplementedException();
        }

        public Instant GetLastPersistedOpenDt(int streamId)
        {
            using var context = sp.CreateScope().ServiceProvider.GetRequiredService<MDTimescaleContext>();
            return context.TimeBAU
                .Where(x => x.StreamId == streamId)
                .OrderByDescending(x => x.OpenDt)
                .FirstOrDefault()?.OpenDt ?? Instant.MinValue;
        }
    }
}

