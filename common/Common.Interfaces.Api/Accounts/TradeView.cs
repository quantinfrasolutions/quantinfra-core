using QuantInfra.Sdk.Trading;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class TradeView : Trade
{
    public string AccountName { get; init; }
    public string ContractName { get; init; }
    
    public TradeView() { }

    public TradeView(Trade t, string accountName, string contractName) : base(t)
    {
        AccountName = accountName;
        ContractName = contractName;
    }
}