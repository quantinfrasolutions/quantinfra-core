using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class AccountViewBrief
{
    public int AccountId { get; init; }
    public AccountType AccountType { get; init; }
    public string AccountName { get; init; }
    
    
    public AccountViewBrief() { }
    
    public AccountViewBrief(int accountId, AccountType accountType, string accountName)
    {
        AccountId = accountId;
        AccountType = accountType;
        AccountName = accountName;
    }
}