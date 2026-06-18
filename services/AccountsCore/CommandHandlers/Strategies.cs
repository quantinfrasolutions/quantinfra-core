using AccountsCore;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Commands.Strategies.AccountsService;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Sdk.Strategies;
using Strategy = QuantInfra.Domain.Strategies.Strategy;

namespace QuantInfra.Services.AccountsCore.CommandHandlers;

public class Strategies(IQueryBus queryBus) :
    ICommandHandler<UpdateLastCalculationTsCmd>,
    ICommandHandler<UpdateStrategyInternalStateCmd>
{
    public void Handle(UpdateLastCalculationTsCmd cmd)
    {
        var strategy = queryBus.Query<GetStrategy, IStrategy?>(new(cmd.StrategyId));
        strategy?.UpdateLastCalculationTs(cmd.LastCalculationTs);
    }

    public void Handle(UpdateStrategyInternalStateCmd cmd)
    {
        var strategy = queryBus.Query<GetStrategyConcreteImplementation, Strategy?>(new(cmd.StrategyId));
        strategy?.UpdateInternalStateFromString(cmd.InternalStateJson);
    }
}