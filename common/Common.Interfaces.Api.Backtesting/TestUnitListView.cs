using NodaTime;

namespace QuantInfra.Common.Interfaces.Api.Backtesting;

public class TestUnitListView
{
    public TestUnitListView(Guid testId, string action, Instant createdAt, Sdk.Backtesting.TestUnitStatus status)
    {
        TestId = testId;
        Action = action;
        CreatedAt = createdAt;
        Status = status;
    }

    public Guid TestId { get; init; }
    public string Action { get; init; }
    public Instant CreatedAt { get; init; }
    public QuantInfra.Sdk.Backtesting.TestUnitStatus Status { get; init; }
}