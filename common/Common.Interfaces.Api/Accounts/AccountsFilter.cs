using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class AccountsFilter
{
    public List<int>? AccountIds { get; set; }
    public string? AccountName { get; set; }
    public List<AccountType>? AccountTypes { get; set; }
    public int? StrategyId { get; set; }
}