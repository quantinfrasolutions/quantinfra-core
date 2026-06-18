using NodaTime;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record OrderReplaceRequestExternalCreatedEvt(
    long EventId,
    int AccountId,
    OrderReplaceRequestExternal Ocr,
    ExecutionReport ExecutionReport,
    long Version,
    Instant Timestamp) : IAccountEventBase;