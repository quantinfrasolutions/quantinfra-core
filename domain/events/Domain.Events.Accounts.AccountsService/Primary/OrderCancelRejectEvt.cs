using NodaTime;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record OrderCancelRejectEvt(
    int AccountId,
    long EventId, 
    OrderCancelReject Ocr,
    Instant Timestamp, 
    long Version
) : IAccountEventBase;