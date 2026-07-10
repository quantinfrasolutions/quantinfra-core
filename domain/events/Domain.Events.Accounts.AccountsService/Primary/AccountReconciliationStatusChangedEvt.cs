using System;
using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary
{
    public record AccountReconciliationStatusChangedEvt(
        long EventId,
        int AccountId,
        long Version,
        bool NeedsReconciliation,
        string? Message,
        Instant Timestamp,
        Guid? RequestId = null
    ) : IAccountEventBase;
}

