// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;
// using Microsoft.EntityFrameworkCore;
// using QuantInfra.Databases.Main.Models.StaticData;
// using Stream = QuantInfra.Databases.Main.Models.StaticData.Stream;
//
// namespace Databases.Main.Models.StaticData
// {
// 	[Table("trading_sessions_streams")]
// 	[PrimaryKey(nameof(StreamId), nameof(TradingSessionId))]
// 	public class TradingSessionsToStreamsMapping
// 	{		
// 		[Column("stream_id")]
// 		[Required]
// 		public long StreamId { get; set; }		
// 		[Column("trading_session_id")]
// 		[Required]
// 		public long TradingSessionId { get; set; }
//
// 		public virtual Stream Stream { get; set; } = default!;
// 		public virtual TradingSession TradingSession { get; set; } = default!;
// 	}
// }
//
