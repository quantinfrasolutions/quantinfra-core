using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.HostedStrategies;

internal class CalculationContext : HostedStrategyCalculationContext
{
    private readonly IQueryBus _queryBus;

    public CalculationContext(Strategy? strategyRecord,
        IStrategyStateReadonly? strategyState,
        IAccountStateReadonly? accountState,
        IStrategy? strategy,
        ITradingAccount? account,
        AccountRecordV6? accountRecord,
        Instant referenceDt,
        Instant processingDt,
        bool isHistory,
        IQueryBus queryBus,
        bool throwOnZeroVolumeOrders = true,
        int virtualAccountSizeStepFraction = 100
    ) : base(strategyRecord, strategyState, accountState, strategy, account, accountRecord, referenceDt, processingDt,
            isHistory, throwOnZeroVolumeOrders, virtualAccountSizeStepFraction)
    {
        _queryBus = queryBus;
    }

    public override Contract? GetContract(int contractId) =>
        _queryBus.Query<GetContract, Contract?>(new GetContract(contractId));
}