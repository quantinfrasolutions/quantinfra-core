using Common.Trading;
using NodaTime;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record TradeEvt(
    long EventId,
    int AccountId,
    Trade Trade,
    long Version,
    Instant Timestamp,
    int AssetId,
    int SettlCcyPrecision,
    SecurityType SecurityType,
    int AccountCcyPrecision
) : IAccountEventBase;