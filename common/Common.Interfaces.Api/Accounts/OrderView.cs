using System.Text.Json.Serialization;
using NodaTime;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
    public class OrderView : OrderStatus
    {
	    public string AccountName { get; set; }
	    public string? BrokerAccountName { get; set; }
	    public string ContractName { get; set; }
        
	    // TODO public OrderView() { }

	    [JsonConstructor]
	    public OrderView(string accountServiceName, long orderId, string? clOrdId, int accountId, int? brokerAccountId,
		    int contractId, string strategyPositionId, PositionEffect? positionEffect, OrdStatus ordStatus,
		    OrdType ordType, Side side, decimal orderQty, decimal cumQty, decimal leavesQty, decimal? price,
		    decimal? stopPx, TimeInForce timeInForce, Instant? expireDt,
		    IReadOnlyDictionary<string, LinkType> linkedOrders, PegInstructions? pegInstructions, bool isSltp,
		    string? externalId, long? executionRequestId, long? signalGroupId, RejectReason? rejectReason,
		    string? rejectText, string accountName, string? brokerAccountName, string contractName
		) : base(accountServiceName, orderId, clOrdId, accountId, brokerAccountId, contractId, strategyPositionId,
			    positionEffect, ordStatus, ordType, side, orderQty, cumQty, leavesQty, price, stopPx, timeInForce,
			    expireDt, linkedOrders, pegInstructions, isSltp, externalId, executionRequestId, signalGroupId,
			    rejectReason, rejectText)
	    {
		    AccountName = accountName;
		    BrokerAccountName = brokerAccountName;
		    ContractName = contractName;
	    }

	    public OrderView(OrderStatus? o, string accountName, string? brokerAccountName, string contractName) : base(o)
	    {
		    AccountName = accountName;
		    BrokerAccountName = brokerAccountName;
		    ContractName = contractName;
	    }
	}
}

