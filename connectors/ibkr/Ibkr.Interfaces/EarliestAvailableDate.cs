using NodaTime;

namespace QuantInfra.Connectors.Ibkr.Interfaces;

public class EarliestAvailableDate
{
    public Instant Ts { get; init; }
}