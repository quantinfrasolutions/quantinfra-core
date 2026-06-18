// using BacktestingCore.ResultWriters;
// using Common.Backtesting;
// using Databases.Optimization;
//
// namespace StrategyTester.ResultWriters
// {
//
//     public class ResultsPersisterFactory
//     {
//         OutputFormatFile _format;
//         private OptimizationContext _optimizationContext;
//         public ResultsPersisterFactory(OutputFormatFile format = OutputFormatFile.Parquet, OptimizationContext optimizationContext = null)
//         {
//             _format = format;
//             _optimizationContext = optimizationContext;
//         }
//         public IResultsPersister CreateResultsPersisterInstance(string trades = null, string pnl = null, 
//             string fitness = null, string positions = null, string commissions = null)
//         {
//             switch (_format)
//             {
//                 case OutputFormatFile.Csv:
//                     return new CsvToStreamsResultsPersister(trades,pnl, positions, fitness, commissions);
//                 case OutputFormatFile.Parquet:
//                     return new ParquetToStreamResultsPersister(trades, pnl, positions, fitness, commissions);
//                 case OutputFormatFile.Postgres:
//                     return new DbResultsPersister(_optimizationContext);
//                 default:
//                     return new CsvToStreamsResultsPersister(trades, pnl, positions, fitness, commissions);
//             }
//         }
//     }
// }