using NodaTime;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
	public class TradeFilter : PagingFilter
	{
		public Instant? FromDt { get; set; }
		public Instant? ToDt { get; set; }
		public int? AccountId { get; set; }
		public long? TradeId { get; set; }
		public int? ContractId { get; set; }
		public string? ExternalId { get; set; }
	}
}

