using NodaTime;

namespace QuantInfra.Tests.Mocks;

public class MockClock : IClock
{
    public Instant CurrentInstant { get; set; }
    public Instant GetCurrentInstant() => CurrentInstant;
}