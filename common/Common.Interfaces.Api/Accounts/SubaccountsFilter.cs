using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class SubaccountsFilter
{
    public SubaccountType? Classifier { get; set; }
}