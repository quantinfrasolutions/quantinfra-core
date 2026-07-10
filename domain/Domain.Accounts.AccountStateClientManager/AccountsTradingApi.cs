using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NodaTime;
using Prometheus;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Domain.Account.Execution.State;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;

namespace QuantInfra.Domain.Accounts.AccountStateClientManager;

public class AccountsTradingApi : AccountsStateManager,
    IQueryHandler<GetAccount, ITradingAccount?>
{
    private readonly IAccountsServiceApi _serviceApi;
    private readonly Histogram? _brokerOrderRoundtrip;
    private readonly Histogram? _accountsServiceRoundtrip;

    public AccountsTradingApi(
        Config config,
        IEventBus eventBus,
        IQueryBus queryBus,
        IAccountsServiceApi serviceApi,
        ILoggerFactory loggerFactory,
        IClock clock
    ) : base(eventBus, queryBus, serviceApi, loggerFactory, clock)
    {
        _serviceApi = serviceApi;

        if (config.WritePerformanceMetrics)
        {
            _brokerOrderRoundtrip = MetricsDefinition.BrokerOrderRoundrtip;
            _accountsServiceRoundtrip = MetricsDefinition.AccountsServiceRoundrtip;
        }
    }
    
    protected override AccountBaseState InstantiateAccount(int accountId, AccountRecordV6 account, AccountStateReadonly receivedState) => receivedState switch
    {
        BrokerAccountStateReadonly ba => new BrokerAccountState(ba.AccountServiceName, ba.AccountId, ba.PositionAccounting, 
            ba.Balances, ba.Orders, ba.Positions, ba.SharePrice, ba.ShareCount, ba.HWM, ba.Investment, ba.RealizedPnLSinceLastMtm,
            ba.Version, ba.LastReconciliationDt, ba.LastReceivedTradeTs, ba.LastReceivedTradeIds, ba.PendingFills.Values,
            ba.LastReceivedBalanceOperationTs, ba.LastReceivedBalanceOperationIds,
            ba.TradesDeadLetterQueue, ba.UnmappedExternalContractIds, ba.UnmappedExternalAssetIds, ba.UsedContractIds, 
            ba.IsReconciled, ba.ReconciliationMessages, ba.NeedsOrdersReconciliation, ba.NeedsTradesReconciliation, 
            EventBus, LoggerFactory),
        _ => new TradingAccount(accountId, receivedState.PositionAccounting,
            receivedState.Balances, receivedState.Orders, receivedState.Positions, receivedState.SharePrice, receivedState.ShareCount, 
            receivedState.HWM, receivedState.Investment, receivedState.RealizedPnLSinceLastMtm, receivedState.Version, 
            account.AccountServiceName, _serviceApi, Clock, EventBus, LoggerFactory, _brokerOrderRoundtrip, _accountsServiceRoundtrip)
    };
    
    internal TradingAccount? GetAccount(int accountId) => Accounts.GetValueOrDefault(accountId) as TradingAccount;
    public ITradingAccount? Handle(GetAccount query) => Accounts.GetValueOrDefault(query.AccountId) as TradingAccount;
}