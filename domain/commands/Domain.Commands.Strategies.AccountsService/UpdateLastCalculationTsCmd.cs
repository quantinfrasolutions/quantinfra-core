using System.Text.Json.Serialization;
using NodaTime;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Commands.Strategies.AccountsService;

public record UpdateLastCalculationTsCmd(
    Guid RequestId,
    int StrategyId,
    Instant LastCalculationTs
) : ICommand
{  
    [JsonConstructor]
    public UpdateLastCalculationTsCmd(int strategyId, Instant lastCalculationTs) : this(Guid.NewGuid(), strategyId, lastCalculationTs) { }
}