using System;
namespace Common.MarketData
{
	[Flags]
	public enum PriceAdjustment
	{
		None = 0,
		AdjustForDividends = 1,
		AdjustForSplits = 2,
		RollFuturesOnExpirationDate = 4,
		RollFuturesOnRolloverDate = 8
	}
}

