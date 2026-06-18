using NodaTime;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record NewOrderSingleExternalCreatedEvt(
    long EventId,
    int AccountId,
    NewOrderSingleExternal Order,
    ExecutionReport ExecutionReport,
    BrokerType BrokerType,
    long Version,
    Instant Timestamp
) : IAccountEventBase;