using Databases.MarketDataHistory;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Sdk.MarketData;

namespace DAL.MarketDataHistory
{
    public class MarketDataHistoryProvider(IServiceProvider sp) :
        IMarketDataHistoryProvider
    {
        // private readonly MDTimescaleContext _context;
        //
        // public MarketDataHistoryProvider(MDTimescaleContext context)
        // {
        //     _context = context;
        // }
        //
        // public IEnumerable<ExchangeBar> GetBAUsByContract(long contractId, Instant from, Instant to)
        // {
        //     return _context.GetBAUsByContract(contractId, from, to).ToList();
        // }
        //
        // public IReadOnlyDictionary<long, double> GetLastKnownPrices(IEnumerable<long> contractIds, Instant dt)
        // {
        //     return _context.GetLastKnownPrices(contractIds, dt);
        // }
        //
        // public IEnumerable<ExchangeBar> GetBAUsByStream(long streamId, Instant from, Instant to)
        // {
        //     return _context.GetBAUsByStream(streamId, from, to).ToList();
        // }

        public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to)
        {
            using var context = sp.CreateScope().ServiceProvider.GetRequiredService<MDTimescaleContext>();
            return context.GetBAUsByContract(contractId, from, to).ToList();
        }

        public IEnumerable<ExchangeBar> GetAggregatedCandlesByContract(int contractId, Instant from, Instant to,
            Period timeframe, string timezone)
        {
            using var context = sp.CreateScope().ServiceProvider.GetRequiredService<MDTimescaleContext>();
            return context.GetAggregatedCandlesByContract(contractId, from, to, timeframe, timezone).ToList();
        }

        public IEnumerable<ExchangeBar> GetAggregatedBausByStream(int streamId, Instant from, Instant to,
            Period timeframe, string timezone)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<int, double> GetLastKnownPrices(IEnumerable<int> contractIds, Instant dt)
        {
            using var context = sp.CreateScope().ServiceProvider.GetRequiredService<MDTimescaleContext>();
            return context.GetLastKnownPrices(contractIds, dt);
        }
        
        public IEnumerable<ExchangeBar> GetBAUsByStream(int streamId, Instant from, Instant to)
        {
            using var context = sp.CreateScope().ServiceProvider.GetRequiredService<MDTimescaleContext>();
            return context.GetBAUsByStream(streamId, from, to).ToList();
        }
    }
}

