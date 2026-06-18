using NodaTime;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
    public class PositionHistoryFilter : ActivePositionFilter
	{
		public Instant? CloseDtFrom { get; set; }
		public Instant? CloseDtTo { get; set; }
		public List<PositionChangeType>? Type { get; set; }
	}
}

