using System.Text.Json.Serialization;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Commands.Strategies.AccountsService;

public record UpdateStrategyInternalStateCmd(
    Guid RequestId,
    int StrategyId,
    string InternalStateJson
) : ICommand
{  
    [JsonConstructor]
    public UpdateStrategyInternalStateCmd(int strategyId, string internalStateJson) : this(Guid.NewGuid(), strategyId, internalStateJson) { }
}