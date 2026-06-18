using NodaTime;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record ExternalExecutionReportEvt(
    long EventId,
    int AccountId,
    long Version,
    BrokerType BrokerType,
    string? ExternalContractId,
    ExecutionReport ExecutionReport,
    Instant Timestamp
) : IAccountEventBase;