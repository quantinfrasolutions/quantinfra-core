using NodaTime;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record OrderCancelRequestExternalCreatedEvt(
    long EventId,
    int AccountId,
    OrderCancelRequestExternal Ocr,
    ExecutionReport ExecutionReport,
    long Version,
    Instant Timestamp) : IAccountEventBase;