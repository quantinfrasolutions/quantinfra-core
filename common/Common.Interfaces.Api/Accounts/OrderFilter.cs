using NodaTime;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
	public class OrderFilter : PagingFilter
	{
		public int? AccountId { get; set; }
		public long? OrderId { get; set; }
		public int? ContractId { get; set; }
		public OrdStatus? OrdStatus { get; set; }
		public string? ExternalId { get; set; }
		public long? ExecutionRequestId { get; set; }
		public Instant? FromDt { get; set; }
		public Instant? ToDt { get; set; }
		public ExecType? ExecType { get; set; }
	}
}

