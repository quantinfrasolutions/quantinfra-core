using System;
using NodaTime;
using NodaTime.Text;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.Trading;

namespace QuantInfra.Connectors.Ibkr.Common
{
	public static class Convertors
	{
		private static readonly LocalDatePattern LocalDatePattern = LocalDatePattern.CreateWithInvariantCulture("yyyyMMdd");
		private static readonly LocalTimePattern LocalTimePattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm:ss");
		// public static string ToIBKRString(this SecurityType securityType) => securityType switch
  //       {
		// 	SecurityType.Unknown => "",
		// 	SecurityType.Stock => "STK",
		// 	SecurityType.Futures => "FUT",
		// 	SecurityType.CFD => "CFD",
		// 	SecurityType.FX => "CASH"
		// };
  //
		// public static SecurityType ToSecurityType(this string securityType) => securityType switch
		// {
		// 	"STK" => SecurityType.Stock,
		// 	"FUT" => SecurityType.Futures,
		// 	"CFD" => SecurityType.CFD,
		// 	"CASH" => SecurityType.FX,
		// 	_ => SecurityType.Unknown
		// };

		public static string ToIBKRString(this SubscriptionType type) => type switch
		{
			SubscriptionType.Trades => "TRADES",
			SubscriptionType.Midpoint => "MIDPOINT"
		};

		public static SubscriptionType ToSubscriptionType(this string s) => s switch
		{
			"TRADES" => SubscriptionType.Trades,
			"MIDPOINT" => SubscriptionType.Midpoint
		};

		public static string ToIBKRString(this Side side) => side switch
		{
			Side.Buy => "BUY",
			Side.Sell => "SELL",
			_ => throw new ArgumentException("Unknown side")
		};

		public static Side ToSide(this string side) => side switch
		{
			"BUY" => Side.Buy,
			"SELL" => Side.Sell,
			_ => Side.Unknown
		};

		public static string ToTimeString(this Instant ts) =>
			$"{ts.ToString("yyyyMMdd HH:mm:ss", null)} UTC";
		
		public static Instant FromTimeString(this string time)
		{
			// 20240611 05:36:11 US/Eastern
			var parts = time.Split(' ');
			var dt = LocalDatePattern.Parse(parts[0]).Value + LocalTimePattern.Parse(parts[1]).Value;
			return dt.InZoneStrictly(DateTimeZoneProviders.Tzdb[parts[2]]).ToInstant();
		}

		// public static string GetExternalId(this IBApi.Contract contract) => contract.ConId.ToString();
	}
}

