using Common.Trading.Positions;
using NodaTime;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Projections;

public record PositionChangedEvt(
    long EventId,
    int AccountId,
    Position? ActualPosition,
    Position? PositionHistoryRecord,
    PositionChangeType Type,
    Instant Timestamp,
    PositionValue? PositionValue = null
) : IAccountProjectionUpdatedEvt;