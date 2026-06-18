using Databases.MarketDataHistory.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Databases.MarketDataHistory;
using QuantInfra.Sdk.MarketData;

namespace Databases.MarketDataHistory
{
    public partial class MDTimescaleContext : MDTimescaleContextDesign, IMarketDataHistoryProvider
    {
        public MDTimescaleContext(MDDatasource? dataSource = null, Config? config = null) : base(dataSource, config)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // HACK
            string s = null;
            modelBuilder.Entity<PriceQueryResult>().ToTable(s);

            modelBuilder
                .HasDbFunction(
                    typeof(MDTimescaleContext).GetMethod(
                        nameof(GetTimeBAUByStreamFromTo),
                        new[]
                        {
                            typeof(long),
                            typeof(Instant),
                            typeof(Instant)
                        }
                    )!
                )
                .HasSchema("public")
                .HasName("get_stream_time_bau_start_end")
                .IsBuiltIn(false);
            
            modelBuilder
                .HasDbFunction(
                    typeof(MDTimescaleContext).GetMethod(
                        nameof(GetTimeBAUByContractFromTo),
                        new[]
                        {
                            typeof(long),
                            typeof(Instant),
                            typeof(Instant)
                        }
                    )!
                )
                .HasSchema("public")
                .HasName("get_contract_time_bau_start_end")
                .IsBuiltIn(false);
            
            modelBuilder
                .HasDbFunction(
                    typeof(MDTimescaleContext).GetMethod(
                        nameof(GetAggregatedContractCandlesStartEnd),
                        new[]
                        {
                            typeof(long),
                            typeof(Instant),
                            typeof(Instant),
                            typeof(Period),
                            typeof(Period),
                            typeof(string)
                        }
                    )!
                )
                .HasSchema("public")
                .HasName("get_aggregated_contract_candles_start_end")
                .IsBuiltIn(false);
            
            modelBuilder
                .HasDbFunction(
                    typeof(MDTimescaleContext).GetMethod(
                        nameof(GetAggregatedStreamCandlesStartEnd),
                        new[]
                        {
                            typeof(long),
                            typeof(Instant),
                            typeof(Instant),
                            typeof(Period),
                            typeof(Period),
                            typeof(string)
                        }
                    )!
                )
                .HasSchema("public")
                .HasName("get_aggregated_stream_candles_start_end")
                .IsBuiltIn(false);
            
            
            modelBuilder
                .HasDbFunction(
                    typeof(MDTimescaleContext).GetMethod(
                        nameof(GetContractsLastPrice),
                        new[]
                        {
                            typeof(long[]),
                            typeof(Instant)
                        }
                    )!
                )
                .HasSchema("public")
                .HasName("get_contracts_last_price")
                .IsBuiltIn(false);
        }

        public IQueryable<PriceQueryResult> GetTimeBAUByStreamFromTo(
            long streamId,
            Instant from,
            Instant to
        ) => FromExpression(
                () => GetTimeBAUByStreamFromTo(streamId, from, to)
            );
        
        public IQueryable<PriceQueryResult> GetTimeBAUByContractFromTo(
            long contractId,
            Instant from,
            Instant to
        ) => FromExpression(
            () => GetTimeBAUByContractFromTo(contractId, from, to)
        );       
        
        public IQueryable<PriceQueryResult> GetAggregatedContractCandlesStartEnd(
            long contractId,
            Instant from,
            Instant to,
            Period timeframe,
            Period offset,
            string timezone
        ) => FromExpression(
            () => GetAggregatedContractCandlesStartEnd(contractId, from, to, timeframe, offset, timezone)
        );       
        
        public IQueryable<PriceQueryResult> GetAggregatedStreamCandlesStartEnd(
            long contractId,
            Instant from,
            Instant to,
            Period timeframe,
            Period offset,
            string timezone
        ) => FromExpression(
            () => GetAggregatedContractCandlesStartEnd(contractId, from, to, timeframe, offset, timezone)
        );   
        

        public IQueryable<ContractsLastPriceResult> GetContractsLastPrice(long[] contractIds, Instant dt) =>
            FromExpression(() => GetContractsLastPrice(contractIds, dt));

        public IReadOnlyDictionary<int, double> GetLastKnownPrices(IEnumerable<int> contractIds, Instant dt) =>
            GetContractsLastPrice(contractIds.Select(cid => (long)cid).ToArray(), dt)
                .ToDictionary(i => (int)i.ContractId, i => i.Close);
        

        public IEnumerable<ExchangeBar> GetBAUsByStream(int streamId, Instant from, Instant to)
        {
            // if (aggType == BarAggregationType.Time)
            return GetTimeBAUByStreamFromTo(streamId, from, to).Select(b => b.ToExchangeBar(null));

            // throw new NotImplementedException();
        }
        

        public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to)
        {
            // if (aggType == BarAggregationType.Time)
            return GetTimeBAUByContractFromTo(contractId, from, to).Select(b => b.ToExchangeBar(null));
            
            // throw new NotImplementedException();
        }

        public IEnumerable<ExchangeBar> GetAggregatedCandlesByContract(int contractId, Instant from, Instant to,
            Period timeframe, string timezone)
        {
            return GetAggregatedContractCandlesStartEnd(contractId, from, to, timeframe, Period.Zero, timezone)
                .Select(b => b.ToExchangeBar(null));
        }

        public IEnumerable<ExchangeBar> GetAggregatedBausByStream(int streamId, Instant from, Instant to,
            Period timeframe, string timezone)
        {
            return GetAggregatedStreamCandlesStartEnd(streamId, from, to, timeframe, Period.Zero, timezone)
                .Select(b => b.ToExchangeBar(null));
        }
    }
}