using System.Text.Json.Serialization;
using NodaTime;
using QuantInfra.Sdk.Trading.ExternalAccounts;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.UserStreams;

class AccountUpdate : BaseStreamMessage
{
    [JsonPropertyName("T")] public long TransactionTime { get; set; }
    [JsonPropertyName("a")] public AccountUpdateData Update { get; set; }

    public IReadOnlyCollection<ExternalBalanceOperation> ToExternalBalanceOperation(int accountId, Instant dt) => Update.Balances
        .Where(b => b.BalanceChange != 0)
        .Select(b => new ExternalBalanceOperation(accountId, b.Asset, b.BalanceChange, 
            $"{TransactionTime}-{Update.EventReasonType}", dt, Update.EventReasonType == EventReasonType.FUNDING_FEE, null))
        .ToList();
}

class AccountUpdateData
{
    [JsonPropertyName("m")] public EventReasonType EventReasonType  { get; set; }
    [JsonPropertyName("B")] public List<BalanceUpdate> Balances  { get; set; }
    [JsonPropertyName("P")] public List<PositionUpdate> Positions  { get; set; }
}

class BalanceUpdate
{
    [JsonPropertyName("a")] public string Asset  { get; set; }
    [JsonPropertyName("wb")] public decimal WalletBalance  { get; set; }
    [JsonPropertyName("cw")] public decimal CrossWalletBalance { get; set; }
    [JsonPropertyName("bc")] public decimal BalanceChange { get; set; } //  except PnL and Commission
}

class PositionUpdate
{
    [JsonPropertyName("s")] public string Symbol { get; set; }
    [JsonPropertyName("pa")] public decimal PositionAmount { get; set; }
    [JsonPropertyName("ep")] public decimal EntryPrice { get; set; }
    [JsonPropertyName("bep")] public decimal BreakevenPrice { get; set; }
    [JsonPropertyName("cr")] public decimal AccumulatedRealized { get; set; }   // (Pre-fee)
    [JsonPropertyName("up")] public decimal UnrealizedPnL { get; set; }
    [JsonPropertyName("iw")] public decimal IsolatedWallet { get; set; }        // (if isolated position)
    [JsonPropertyName("ps")] public string PositionSide { get; set; }
}

enum EventReasonType
{
    DEPOSIT,
    WITHDRAW,
    ORDER,
    FUNDING_FEE,
    WITHDRAW_REJECT,
    ADJUSTMENT,
    INSURANCE_CLEAR,
    ADMIN_DEPOSIT,
    ADMIN_WITHDRAW,
    MARGIN_TRANSFER,
    MARGIN_TYPE_CHANGE,
    ASSET_TRANSFER,
    OPTIONS_PREMIUM_FEE,
    OPTIONS_SETTLE_PROFIT,
    AUTO_EXCHANGE,
    COIN_SWAP_DEPOSIT,
    COIN_SWAP_WITHDRAW,
}