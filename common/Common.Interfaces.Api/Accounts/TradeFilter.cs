using NodaTime;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
	public class TradeFilter : PagingFilter
	{
		public long? FromDt { get; set; }
		public long? ToDt { get; set; }
		public int? AccountId { get; set; }
		public long? TradeId { get; set; }
		public int? ContractId { get; set; }
		public string? ExternalId { get; set; }
	}
}

