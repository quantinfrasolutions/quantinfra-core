using NodaTime;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
    public class ActivePositionFilter
	{
		public long? OpenDtFrom { get; set; }
		public long? OpenDtTo { get; set; }
		public long? HistoryOpenDtFrom { get; set; }
		public long? HistoryOpenDtTo { get; set; }
		public int? AccountId { get; set; }
		public int? ContractId { get; set; }
		public long? TradeId { get; set; }
	}
}

