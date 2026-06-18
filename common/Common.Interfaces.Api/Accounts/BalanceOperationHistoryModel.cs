using NodaTime;
using QuantInfra.Sdk.Accounting;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class BalanceOperationHistoryModel : BalanceOperation
{
    public string AccountName { get; set; }
    public string AssetName { get; set; }
    
    public BalanceOperationHistoryModel() { }

    public BalanceOperationHistoryModel(string accountServiceName, int balanceOperationId, int accountId, Instant dt, decimal amount, int assetId, decimal price, decimal fxRate, decimal valueInAccountCcy, string? externalId, string? description, bool isCorrection, bool affectsPnL, bool affectsInvestment, bool affectsBalance, bool affectsShareCount, string accountName, string assetName) : base(accountServiceName, balanceOperationId, accountId, dt, amount, assetId, price, fxRate, valueInAccountCcy, externalId, description, isCorrection, affectsPnL, affectsInvestment, affectsBalance, affectsShareCount)
    {
        AccountName = accountName;
        AssetName = assetName;
    }
}