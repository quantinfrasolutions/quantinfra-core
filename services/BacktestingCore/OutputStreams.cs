//using System.IO;
//using StrategyTester.Models;

//namespace StrategyTester
//{
//    public class OutputStreams
//	{
//        public IResultsWriter<TradeRecord> TradesWriter { get; set; }
//		public Stream Trades { get; set;} = Stream.Null;
//        public Stream Pl { get; set; } = Stream.Null;
//        public Stream PositionCloses { get; set; } = Stream.Null;
//        public Stream Fitness { get; set; } = Stream.Null;
//        public Stream Equity { get; set; } = Stream.Null;

//		public void CopyTo(OutputStreams target, bool flush = true)
//		{
//			if(Trades != Stream.Null && target.Trades != Stream.Null)
//			{
//				Trades.Position = 0;
//				Trades.CopyTo(target.Trades);
//				if (flush) target.Trades.Flush();
//			}
//            if (Pl != Stream.Null && target.Pl != Stream.Null)
//            {
//                Pl.Position = 0;
//                Pl.CopyTo(target.Pl);
//                if (flush) target.Pl.Flush();
//            }
//            if (PositionCloses != Stream.Null && target.PositionCloses != Stream.Null)
//            {
//                PositionCloses.Position = 0;
//                PositionCloses.CopyTo(target.PositionCloses);
//                if (flush) target.PositionCloses.Flush();
//            }
//            if (Fitness != Stream.Null && target.Fitness != Stream.Null)
//            {
//                Fitness.Position = 0;
//                Fitness.CopyTo(target.Fitness);
//                if (flush) target.Fitness.Flush();
//            }
//            if (Equity != Stream.Null && target.Equity != Stream.Null)
//            {
//                Equity.Position = 0;
//                Equity.CopyTo(target.Equity);
//                if (flush) target.Equity.Flush();
//            }
//        }

        
//    }
//}

