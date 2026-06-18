using NodaTime;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Events.Accounts.External;

public record ExternalOrderCancelRejectEvt(int AccountId, OrderCancelReject Ocr, Instant Timestamp) : IExternalAccountEvent
{
    public long EventId { get; } = 0;
}