using NodaTime;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record ExecutionReportEvt(
    long EventId,
    int AccountId,
    long Version,
    AccountType AccountType,
    ExecutionReport ExecutionReport,
    Instant Timestamp
) : IAccountEventBase;