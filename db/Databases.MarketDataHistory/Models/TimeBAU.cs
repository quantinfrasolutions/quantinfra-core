using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Databases.MarketDataHistory.Models
{
	[Table("time_bau")]
	public class TimeBAU : CandlesHistory
	{
		[Column("trading_session_id")]
		public int? TradingSessionId { get; set; }

		public static ModelBuilder CreateRelations(ModelBuilder modelBuilder)
		{
            // indices are created in migrations SQL
            return modelBuilder;
        }
	}
}

