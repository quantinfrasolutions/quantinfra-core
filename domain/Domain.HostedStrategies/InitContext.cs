using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.HostedStrategies;

internal sealed class InitContext : StrategyInitializationContext
{
    private readonly IQueryBus _queryBus;

    public InitContext(AccountRecordV6 account, IQueryBus queryBus) : base(account)
    {
        _queryBus = queryBus;
    }

    public override Contract GetContract(int contractId) => _queryBus.Query<GetContract, Contract>(new(contractId));
}