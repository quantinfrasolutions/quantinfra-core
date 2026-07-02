using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace Databases.MarketDataHistory.Models
{
	[Keyless]
	public class PriceQueryResult
	{
		[Column("stream_id")]
		public long StreamId { get; init; }
		[Column("open_dt")]
		public Instant OpenDt { get; init; }
		[Column("close_dt")]
        public Instant CloseDt { get; init; }
		[Column("open")]
        public double Open { get; init; }
		[Column("high")]
        public double High { get; init; }
		[Column("low")]
        public double Low { get; init; }
		[Column("close")]
        public double Close { get; init; }
		[Column("face_volume")]
        public double FaceVolume { get; init; }
		[Column("dollar_value")]
        public double DollarValue { get; init; }
		[Column("trading_session_id")]
		public int? TradingSessionId { get; init; }

		public QuantInfra.Sdk.MarketData.ExchangeBar ToExchangeBar(int? contractId = null) => new(
			(int)StreamId, contractId,
			OpenDt, CloseDt, Open, High, Low, Close, FaceVolume, DollarValue, 
			TradingSessionId
		);
	}
}