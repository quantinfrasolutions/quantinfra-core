using NodaTime;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
    public class PositionHistoryFilter : ActivePositionFilter
	{
		public long? CloseDtFrom { get; set; }
		public long? CloseDtTo { get; set; }
		public List<PositionChangeType>? Type { get; set; }
	}
}

