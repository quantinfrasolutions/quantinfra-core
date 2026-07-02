// using Common.MarketData;
// using ParquetSharp;
//
// namespace ParquetBarsStorage;
//
// public static class ParquetWriter
// {
//     public static void Write(string filePath, IEnumerable<ExchangeBar> bars)
//     {
//         var columns = new Column[]
//         {
//             new Column<long>("OpenDt"),
//             new Column<double>("Open"),
//             new Column<double>("High"),
//             new Column<double>("Low"),
//             new Column<double>("Close"),
//             new Column<double>("Volume"),
//         };
//         using var fileWriter = new ParquetFileWriter(filePath, columns);
//         using var rowGroup = fileWriter.AppendRowGroup();
//
//         var data = bars.ToList();
//         
//         using (var writer = rowGroup.NextColumn().LogicalWriter<long>())
//         {
//             writer.WriteBatch(data.Select(b => b.OpenDt.ToUnixTimeSeconds()).ToArray());
//         }
//         using (var writer = rowGroup.NextColumn().LogicalWriter<double>())
//         {
//             writer.WriteBatch(data.Select(b => b.Open).ToArray());
//         }
//         using (var writer = rowGroup.NextColumn().LogicalWriter<double>())
//         {
//             writer.WriteBatch(data.Select(b => b.High).ToArray());
//         }
//         using (var writer = rowGroup.NextColumn().LogicalWriter<double>())
//         {
//             writer.WriteBatch(data.Select(b => b.Low).ToArray());
//         }
//         using (var writer = rowGroup.NextColumn().LogicalWriter<double>())
//         {
//             writer.WriteBatch(data.Select(b => b.Close).ToArray());
//         }
//         using (var writer = rowGroup.NextColumn().LogicalWriter<double>())
//         {
//             writer.WriteBatch(data.Select(b => b.Volume).ToArray());
//         }
//         
//         fileWriter.Close();
//     }
// }