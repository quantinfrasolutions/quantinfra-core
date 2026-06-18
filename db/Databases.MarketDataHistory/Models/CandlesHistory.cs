using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Databases.MarketDataHistory.Models
{
	[Keyless]
	public abstract class CandlesHistory
	{
        [Column("stream_id")]
		public long StreamId { get; set; }
		[Column("open_dt")]
		public Instant OpenDt { get; set; }
        [Column("close_dt")]
        public Instant CloseDt { get; set; }
		[Column("open")]
		public double Open { get; set; }
        [Column("high")]
        public double High { get; set; }
        [Column("low")]
        public double Low { get; set; }
        [Column("close")]
        public double Close { get; set; }
        [Column("face_volume")]
        public double FaceVolume { get; set; }
        [Column("dollar_value")]
        public double DollarValue { get; set; }
	}
}

