using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Databases.Main;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Databases.MarketDataHistory.DAL
{
    public class MarketDataHistoryProvider(IServiceProvider sp) :
        IMarketDataHistoryProvider
    {
        public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to)
        {
            using var scope = sp.CreateScope();
            using var mainContext = scope.ServiceProvider.GetRequiredService<MainContext>();
            var streamId = GetDefaultStreamByContractId(contractId, mainContext);
            
            using var context = sp.CreateScope().ServiceProvider.GetRequiredService<MDTimescaleContext>();
            return context.GetBauByStream(streamId, contractId, from, to).ToList();
        }

        public IEnumerable<ExchangeBar> GetAggregatedCandlesByContract(int contractId, Instant from, Instant to,
            Period timeframe, string timezone)
        {
            using var scope = sp.CreateScope();
            using var mainContext = scope.ServiceProvider.GetRequiredService<MainContext>();
            var streamId = GetDefaultStreamByContractId(contractId, mainContext);
            
            using var context = sp.CreateScope().ServiceProvider.GetRequiredService<MDTimescaleContext>();
            return context.GetAggregatedBarsByStream(streamId, contractId, from, to, timeframe, timezone).ToList();
        }

        public IEnumerable<ExchangeBar> GetAggregatedBausByStream(int streamId, Instant from, Instant to,
            Period timeframe, string timezone)
        {
            using var scope = sp.CreateScope();
            using var mainContext = scope.ServiceProvider.GetRequiredService<MainContext>();
            var contractId = GetContractIdByStreamId(streamId, mainContext);
            
            using var context = scope.ServiceProvider.GetRequiredService<MDTimescaleContext>();
            return context.GetAggregatedBarsByStream(streamId, contractId, from, to, timeframe, timezone).ToList();
        }
        
        public IEnumerable<ExchangeBar> GetBAUsByStream(int streamId, Instant from, Instant to)
        {
            using var scope = sp.CreateScope();
            using var mainContext = scope.ServiceProvider.GetRequiredService<MainContext>();
            var contractId = GetContractIdByStreamId(streamId, mainContext);
            
            using var context = scope.ServiceProvider.GetRequiredService<MDTimescaleContext>();
            return context.GetBauByStream(streamId, contractId, from, to).ToList();
        }
        
        public IReadOnlyDictionary<int, double> GetLastKnownPrices(IEnumerable<int> contractIds, Instant dt)
        {
            throw new NotSupportedException();
        }

        private int GetDefaultStreamByContractId(int contractId, MainContext context)
        {
            var contract = context.Contracts
                .Include(c => c.Streams.Where(s => s.DatafeedId == s.Contract.DefaultDatafeedId))
                .AsNoTracking()
                .SingleOrDefault(c => c.ContractId == contractId);
            if (contract == null) throw new InvalidOperationException($"Contract {contractId} not found");
            var stream = contract.Streams.SingleOrDefault();
            if (stream == null) throw new InvalidOperationException($"Default stream is not configured for contract {contractId}");
            return stream.StreamId;
        }

        private int? GetContractIdByStreamId(int streamId, MainContext context)
        {
            var stream = context.Streams
                .Include(s => s.Contract)
                .AsNoTracking()
                .SingleOrDefault(s => s.StreamId == streamId);

            if (stream is null) throw new InvalidOperationException($"Stream {streamId} not found");
            if (stream.Contract is null) return null;
            return stream.DatafeedId == stream.Contract.DefaultDatafeedId
                ? stream.Contract.ContractId
                : null;
        }
    }
}

