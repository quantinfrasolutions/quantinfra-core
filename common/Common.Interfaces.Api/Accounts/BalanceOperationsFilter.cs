using NodaTime;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
    public class BalanceOperationsFilter : PagingFilter
	{
		public int? AccountId { get; set; }
        public long? BalanceOperationId { get; set; }
		public Instant? FromDt { get; set; }
		public Instant? ToDt { get; set; }
        public string? ExternalId { get; set; }        
    }
}

