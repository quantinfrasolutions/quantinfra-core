using NodaTime;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
    public class ActivePositionFilter
	{
		public Instant? OpenDtFrom { get; set; }
		public Instant? OpenDtTo { get; set; }
		public Instant? HistoryOpenDtFrom { get; set; }
		public Instant? HistoryOpenDtTo { get; set; }
		public int? AccountId { get; set; }
		public int? ContractId { get; set; }
		public long? TradeId { get; set; }
	}
}

