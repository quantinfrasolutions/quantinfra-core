using Microsoft.EntityFrameworkCore;
using NodaTime;
using QuantInfra.Databases.MarketDataHistory.Models;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Databases.MarketDataHistory
{
    public partial class MDTimescaleContext : MDTimescaleContextDesign
    {
        public MDTimescaleContext(MDDatasource? dataSource = null, Config? config = null) : base(dataSource, config)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .HasDbFunction(
                    typeof(MDTimescaleContext).GetMethod(
                        nameof(GetTimeBauByStreamFromTo),
                        new[]
                        {
                            typeof(int),
                            typeof(Instant),
                            typeof(Instant)
                        }
                    )!
                )
                .HasSchema("data")
                .HasName("get_stream_time_bau_start_end")
                .IsBuiltIn(false);
            
            modelBuilder
                .HasDbFunction(
                    typeof(MDTimescaleContext).GetMethod(
                        nameof(GetAggregatedStreamCandlesStartEnd),
                        new[]
                        {
                            typeof(int),
                            typeof(Instant),
                            typeof(Instant),
                            typeof(Period),
                            typeof(Period),
                            typeof(string)
                        }
                    )!
                )
                .HasSchema("data")
                .HasName("get_aggregated_stream_candles_start_end")
                .IsBuiltIn(false);
        }

        public IQueryable<PriceQueryResult> GetTimeBauByStreamFromTo(
            int streamId,
            Instant from,
            Instant to
        ) => FromExpression(
                () => GetTimeBauByStreamFromTo(streamId, from, to)
            );
        
        public IQueryable<PriceQueryResult> GetAggregatedStreamCandlesStartEnd(
            int streamId,
            Instant from,
            Instant to,
            Period timeframe,
            Period offset,
            string timezone
        ) => FromExpression(
            () => GetAggregatedStreamCandlesStartEnd(streamId, from, to, timeframe, offset, timezone)
        );   
        

        public IEnumerable<ExchangeBar> GetBauByStream(int streamId, int? contractId, Instant from, Instant to)
        {
            return GetTimeBauByStreamFromTo(streamId, from, to)
                .Select(b => b.ToExchangeBar(contractId));
        }

        public IEnumerable<ExchangeBar> GetAggregatedBarsByStream(int streamId, int? contractId, Instant from, Instant to,
            Period timeframe, string timezone)
        {
            return GetAggregatedStreamCandlesStartEnd(streamId, from, to, timeframe, Period.Zero, timezone)
                .Select(b => b.ToExchangeBar(contractId));
        }
    }
}