using NodaTime;
using QuantInfra.Sdk.Accounting;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class BalanceValueView : BalanceValue
{
    public BalanceValueView(
        int accountId,
        int currencyId,
        string assetName,
        Instant dt,
        decimal cashBalance,
        decimal holdings,
        decimal unrealizedPnL,
        decimal futuresVariationMargin,
        decimal totalBalance,
        decimal totalValue,
        decimal fxRate
    ) : base(accountId, currencyId, dt, cashBalance, holdings, unrealizedPnL, futuresVariationMargin, totalBalance, totalValue, fxRate)
    {
        AssetName = assetName;
    }

    public BalanceValueView(BalanceValue v, string assetName) : base(v)
    {
        AssetName = assetName;
    }
    
    public string AssetName { get; init; }
}