// using Common.MarketData;
// using NodaTime;
// using ParquetSharp;
//
// namespace ParquetBarsStorage;
//
// public class ParquetFileHistoryViewer
// {
//     private ParquetFileReader _reader;
//     private readonly Duration _tf;
//
//     public ParquetFileHistoryViewer(string path, Period? storageTf = null)
//     {
//         _reader = new ParquetFileReader(path);
//         _tf = (storageTf ?? Period.FromMinutes(1)).ToDuration();
//     }
//
//     public long GetLastFileDt()
//     {
//         return ReadFile((rg, rows) => rg.Column(0).LogicalReader<long>().ReadAll(rows).Last()).Max();
//     }
//
//     public long GetFirstFileDt()
//     {
//         return ReadFile((rg, rows) => rg.Column(0).LogicalReader<long>().ReadAll(1).First()).Min();
//     }
//
//     public IEnumerable<ExchangeBar> GetBars(long fromTs, long toTs) =>
//         ReadFileMult((rg, rows) =>
//         {
//             var barsBatched = new List<ExchangeBar>(rows);
//             var dts = rg.Column(0).LogicalReader<long>().ReadAll(rows);
//             var opens = rg.Column(1).LogicalReader<double>().ReadAll(rows);
//             var highs = rg.Column(2).LogicalReader<double>().ReadAll(rows);
//             var lows = rg.Column(3).LogicalReader<double>().ReadAll(rows);
//             var closes = rg.Column(4).LogicalReader<double>().ReadAll(rows);
//             var volumes = rg.Column(5).LogicalReader<double>().ReadAll(rows);
//
//             for (var i = 0; i < dts.Length; i++)
//             {
//                 var dt = dts[i];
//                 if (dt < fromTs || dt >= toTs) continue;
//                 var openDt = Instant.FromUnixTimeSeconds(dts[i]);
//                 barsBatched.Add(new ExchangeBar
//                 {
//                     StreamId = 10000,
//                     OpenDt = openDt,
//                     CloseDt = openDt.Plus(_tf),
//                     Open = (double)opens[i],
//                     High = (double)highs[i],
//                     Low = (double)lows[i],
//                     Close = (double)closes[i],
//                     Volume = (int)volumes[i],
//                     // TradingSessionId = ts?.Id
//                 });
//             }
//
//             return barsBatched;
//         });
//     
//     public Task<IEnumerable<ExchangeBar>> GetBarsAsync(long fromTs, long toTs) =>
//         ReadFileMultAsync((rg, rows) =>
//         {
//             var barsBatched = new List<ExchangeBar>(rows);
//             var dts = rg.Column(0).LogicalReader<long>().ReadAll(rows);
//             var opens = rg.Column(1).LogicalReader<double>().ReadAll(rows);
//             var highs = rg.Column(2).LogicalReader<double>().ReadAll(rows);
//             var lows = rg.Column(3).LogicalReader<double>().ReadAll(rows);
//             var closes = rg.Column(4).LogicalReader<double>().ReadAll(rows);
//             var volumes = rg.Column(5).LogicalReader<double>().ReadAll(rows);
//
//             for (var i = 0; i < dts.Length; i++)
//             {
//                 var dt = dts[i];
//                 if (dt < fromTs || dt >= toTs) continue;
//                 var openDt = Instant.FromUnixTimeSeconds(dts[i]);
//                 barsBatched.Add(new ExchangeBar
//                 {
//                     StreamId = 10000,
//                     OpenDt = openDt,
//                     CloseDt = openDt.Plus(_tf),
//                     Open = (double)opens[i],
//                     High = (double)highs[i],
//                     Low = (double)lows[i],
//                     Close = (double)closes[i],
//                     Volume = (int)volumes[i],
//                     // TradingSessionId = ts?.Id
//                 });
//             }
//
//             return barsBatched;
//         });
//     
//     
//     private IEnumerable<TVal> ReadFileMult<TVal>(Func<RowGroupReader, int, IEnumerable<TVal>> readGroup)
//     {
//         var res = new List<IEnumerable<TVal>>(_reader.FileMetaData.NumRowGroups);
//         for (int rowGroup = 0; rowGroup < _reader.FileMetaData.NumRowGroups; rowGroup++)
//         {
//             using var rowGroupReader = _reader.RowGroup(rowGroup);
//             var groupNumRows = checked((int)rowGroupReader.MetaData.NumRows);
//             res.Add(readGroup(rowGroupReader, groupNumRows));
//         }
//
//         return res.SelectMany(x => x);
//     }
//     
//     private async Task<IEnumerable<TVal>> ReadFileMultAsync<TVal>(Func<RowGroupReader, int, IEnumerable<TVal>> readGroup)
//     {
//         var res = await Task.WhenAll(Enumerable.Range(0, _reader.FileMetaData.NumRowGroups).Select(rowGroup => Task.Run(() =>
//         {
//             using var rowGroupReader = _reader.RowGroup(rowGroup);
//             var groupNumRows = checked((int)rowGroupReader.MetaData.NumRows);
//             return readGroup(rowGroupReader, groupNumRows);
//         })));
//
//         return res.SelectMany(x => x);
//     }
//     
//     private IEnumerable<TVal> ReadFile<TVal>(Func<RowGroupReader, int, TVal> readGroup)
//     {
//         var res = new List<TVal>(_reader.FileMetaData.NumRowGroups);
//         for (int rowGroup = 0; rowGroup < _reader.FileMetaData.NumRowGroups; rowGroup++)
//         {
//             using var rowGroupReader = _reader.RowGroup(rowGroup);
//             var groupNumRows = checked((int)rowGroupReader.MetaData.NumRows);
//             res.Add(readGroup(rowGroupReader, groupNumRows));
//         }
//
//         return res;
//     }
//     
//     private async Task<IEnumerable<TVal>> ReadFileAsync<TVal>(Func<RowGroupReader, int, TVal> readGroup)
//     {
//         var res = await Task.WhenAll(Enumerable.Range(0, _reader.FileMetaData.NumRowGroups).Select(rowGroup => Task.Run(() =>
//         {
//             using var rowGroupReader = _reader.RowGroup(rowGroup);
//             var groupNumRows = checked((int)rowGroupReader.MetaData.NumRows);
//             return readGroup(rowGroupReader, groupNumRows);
//         })));
//
//         return res;
//     }
// }