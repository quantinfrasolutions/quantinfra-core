using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class CreateSubaccountRequest
{
    public int AccountId { get; set; }
    public int SubaccountId { get; set; }
    public SubaccountType Classifier { get; set; }
}