using NodaTime;
using Parquet;
using QuantInfra.Common.MarketData;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Backtesting.ParquetBarsStorage
{
    public class ParquetFileMarketDataHistoryProvider : IMarketDataHistoryProvider
    {
        protected readonly string Path;
        private readonly int _streamId;
        private int? _contractId;
        private readonly Duration _tf;
        private IReadOnlyDictionary<int, IReadOnlyCollection<TradingSession>> _tradingSessions;
        private IReadOnlyDictionary<int, DateTimeZone> _timeZones;
        
        /// <param name="path">Path of the file to read</param>
        /// <param name="streamId">StreamId that will be populated to ExchangeBars</param>
        /// <param name="contractId">ContractId that will be populated to ExchangeBars</param>
        /// <param name="tf">Timeframe of bars stored in the file</param>
        /// <param name="tradingSessions">ContractId => TradingSessions</param>
        public ParquetFileMarketDataHistoryProvider(
            string path, 
            int streamId, 
            int? contractId,
            Duration? tf = null, 
            IReadOnlyDictionary<int, IReadOnlyCollection<TradingSession>> tradingSessions = null
        )
        {
            Path = path;
            _streamId = streamId;
            _contractId = contractId;
            _tf = tf ?? Duration.FromMinutes(1);
            _tradingSessions = tradingSessions?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new();
        }

        private List<ExchangeBar> ReadBars(ParquetReader file, Instant to, Instant? from = null)
        {
            var fields = file.Schema.GetDataFields();
            
            for (int rowGroup = 0; rowGroup < file.RowGroupCount; ++rowGroup)
            {
                using var rowGroupReader = file.OpenRowGroupReader(rowGroup);
                var groupNumRows = checked((int)rowGroupReader.RowCount);

                var dates = ((long[])rowGroupReader.ReadColumnAsync(fields[0]).Result.Data)  // .LogicalReader<long>().ReadAll(groupNumRows)
                    .Select(Instant.FromUnixTimeSeconds).ToList();
                var dts = GetFilteredDates(dates, to, from);

                TradingSessionWatcher<int>? tsw = null;
                if (_tradingSessions is { Count: > 0 })
                {
                    tsw = new TradingSessionWatcher<int>(_tradingSessions);
                }

                if (dts.Count > 0)
                {
                    var barsBatched = new List<ExchangeBar>(dts.Count);
                    var first = dts.First();
                    var startIndex = dates.FindIndex(i => i == first);
                    var opens = (double[])rowGroupReader.ReadColumnAsync(fields[1]).Result.Data; //rowGroupReader.Column(1).LogicalReader<double>().ReadAll(groupNumRows);
                    var highs = (double[])rowGroupReader.ReadColumnAsync(fields[2]).Result.Data;//rowGroupReader.Column(2).LogicalReader<double>().ReadAll(groupNumRows);
                    var lows = (double[])rowGroupReader.ReadColumnAsync(fields[3]).Result.Data; //rowGroupReader.Column(3).LogicalReader<double>().ReadAll(groupNumRows);
                    var closes = (double[])rowGroupReader.ReadColumnAsync(fields[4]).Result.Data; //rowGroupReader.Column(4).LogicalReader<double>().ReadAll(groupNumRows);
                    var volumes = (double[])rowGroupReader.ReadColumnAsync(fields[5]).Result.Data; //rowGroupReader.Column(5).LogicalReader<double>().ReadAll(groupNumRows);

                    for (var i = 0; i < dts.Count; i++)
                    {
                        var openDt = dts[i];
                        var ts = tsw?.ProcessUpdateAndGetCurrentSessionId(_streamId, openDt);
                        barsBatched.Add(new ExchangeBar(_streamId, _contractId, openDt, openDt.Plus(_tf),
                            opens[startIndex + i], highs[startIndex + i], lows[startIndex + i], closes[startIndex + i],
                            volumes[startIndex + i], 0, ts?.TradingSessionId
                        ));
                    }
                    
                    return barsBatched;
                }
            }
            return new List<ExchangeBar>();
        }
        
        private List<ExchangeBar> ReadAggregatedBars(ParquetReader file, Instant to, Period timeframe, string timezone, Instant? from = null)
        {
            var fields = file.Schema.GetDataFields();
            var toUnixSec = to.ToUnixTimeSeconds();
            var fromUnixSec = from?.ToUnixTimeSeconds() ?? Instant.MinValue.ToUnixTimeMilliseconds();
            var tf = timeframe.ToDuration();
            
            for (int rowGroup = 0; rowGroup < file.RowGroupCount; ++rowGroup)
            {
                using var rowGroupReader = file.OpenRowGroupReader(rowGroup);
                var groupNumRows = checked((int)rowGroupReader.RowCount);

                var tss = (long[])rowGroupReader.ReadColumnAsync(fields[0]).Result.Data;
                if (tss.Length == 0 || tss[^1] < fromUnixSec || tss[0] > toUnixSec) continue;
                
                var dates = tss.Select(Instant.FromUnixTimeSeconds).ToList();
                var dts = GetFilteredDates(dates, to, from);

                TradingSessionWatcher<int>? tsw = null;
                if (_tradingSessions is { Count: > 0 })
                {
                    tsw = new TradingSessionWatcher<int>(_tradingSessions);
                }

                if (dts.Count > 0)
                {
                    // var barsBatched = new List<ExchangeBar>(dts.Count / (int)timeframe.ToDuration().TotalMinutes + 1);
                    var bs = new AggregatingBarStorage(null, 
                        (_contractId.HasValue ? _tradingSessions.GetValueOrDefault(_contractId.Value) : null) 
                            ?? new List<TradingSession>(),
                        new BarStorageConfig()
                        {
                            AggregationType = BarAggregationType.Time, Id = _streamId, IdType = IdType.Stream,
                            LastValueOnly = false, Timeframe = timeframe, Timezone = timezone,
                        }, _streamId);
                    bs.InitiateStorage(dts.Count / (int)timeframe.ToDuration().TotalMinutes * (int)_tf.TotalMinutes + 1);
                    
                    var first = dts.First();
                    var startIndex = dates.FindIndex(i => i == first);
                    var opens = (double[])rowGroupReader.ReadColumnAsync(fields[1]).Result.Data;
                    var highs = (double[])rowGroupReader.ReadColumnAsync(fields[2]).Result.Data;
                    var lows = (double[])rowGroupReader.ReadColumnAsync(fields[3]).Result.Data;
                    var closes = (double[])rowGroupReader.ReadColumnAsync(fields[4]).Result.Data;
                    var volumes = (double[])rowGroupReader.ReadColumnAsync(fields[5]).Result.Data;

                    for (var i = 0; i < dts.Count; i++)
                    {
                        var openDt = dts[i];
                        var ts = tsw?.ProcessUpdateAndGetCurrentSessionId(_streamId, openDt);
                        var bau = new ExchangeBar(_streamId, _contractId, openDt, openDt.Plus(tf),
                            opens[startIndex + i], highs[startIndex + i], lows[startIndex + i], closes[startIndex + i],
                            volumes[startIndex + i], 0, ts?.TradingSessionId
                        );
                        bs.AppendBar(bau);
                    }
                    return Enumerable.Range(0, bs.Count).Select(i => bs[i].ToExchangeBar(_streamId, _contractId)).Reverse().ToList();
                }
            }
            return new List<ExchangeBar>();
        }

        private List<Instant> GetFilteredDates(IEnumerable<Instant> dates, Instant to, Instant? from)
        {
            var adjTo = to.Minus(_tf);
            return from == null
                ? dates.Where(dt => dt <= adjTo).ToList()
                : dates.Where(dt => (dt >= from) && (dt <= adjTo)).ToList();
        }
        
        

        public IEnumerable<ExchangeBar> GetBAUsByContract(int contractId, Instant from, Instant to)
        {
            if (contractId != _contractId) throw new ArgumentException("Contract id must be equal to contract id");
            using var file = ParquetReader.CreateAsync(Path).Result;
            var bars = ReadBars(file, to, from);
            // file.Close();
            return bars;
        }

        public IEnumerable<ExchangeBar> GetAggregatedCandlesByContract(int contractId, Instant from, Instant to,
            Period timeframe, string timezone)
        {
            if (contractId != _contractId) throw new ArgumentException("Contract id must be equal to contract id");
            using var file = ParquetReader.CreateAsync(Path).Result;
            var bars = ReadAggregatedBars(file, to, timeframe, timezone, from);
            // file.Close();
            return bars;
        }

        public IEnumerable<ExchangeBar> GetAggregatedBausByStream(int streamId, Instant from, Instant to,
            Period timeframe, string timezone)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<int, double> GetLastKnownPrices(IEnumerable<int> contractIds, Instant dt)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ExchangeBar> GetBAUsByStream(int streamId, Instant from, Instant to)
        {
            throw new System.NotImplementedException();
        }
    }
}

