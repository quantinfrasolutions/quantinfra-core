using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Common.Interfaces.Api.Accounts;

public class PositionValueView
{
    public Position Position { get; init; }
    public PositionValue PositionValue { get; init; }
}