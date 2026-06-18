using System.Collections.Generic;
using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Common.MarketData.InMemoryAggregation
{
    public class BarRequest
	{
		public List<ExchangeBar> ReceivedBars { get; set; }
		public IdType IdType { get; set; }
		public int Id { get; set; }
		public LocalDate? From { get; set; }
		public int CompletedAttempts { get; set; }
		public Instant BausFrom { get; set; }
		public Instant AggregatedBarsFrom { get; set; }
		public bool IsEnoughBars { get; set; }
		public int NumberOfBaus { get; set; }
		public Period MinResolution { get; set; }
		public string Timezone { get; set; }
	}
}

