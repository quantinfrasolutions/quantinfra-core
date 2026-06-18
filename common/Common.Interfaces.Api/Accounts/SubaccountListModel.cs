using System.Text.Json.Serialization;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class SubaccountListModel : Subaccount
{
    [JsonConstructor] public SubaccountListModel() { }

    public SubaccountListModel(Subaccount s, string accountName, string subaccountName, string? brokerName) : base(s)
    {
        AccountName = accountName;
        SubaccountName = subaccountName;
        BrokerName = brokerName;
    }
    
    public string AccountName { get; init; }
    public string SubaccountName { get; init; }
    public string? BrokerName { get; init; }
}