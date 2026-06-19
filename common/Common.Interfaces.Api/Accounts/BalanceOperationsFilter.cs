using NodaTime;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
    public class BalanceOperationsFilter : PagingFilter
	{
		public int? AccountId { get; set; }
        public long? BalanceOperationId { get; set; }
		public long? FromDt { get; set; }
		public long? ToDt { get; set; }
        public string? ExternalId { get; set; }        
    }
}

