using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
    public class OrderHistoryView : ExecutionReport
    {
	    public string AccountName { get; set; }
	    public string? BrokerAccountName { get; set; }
	    public string ContractName { get; set; }
        
	    public OrderHistoryView() { }
		
	    public OrderHistoryView(ExecutionReport o, string accountName, string? brokerAccountName, string contractName) : base(o)
	    {
		    AccountName = accountName;
		    BrokerAccountName = brokerAccountName;
		    ContractName = contractName;
	    }
	}
}

