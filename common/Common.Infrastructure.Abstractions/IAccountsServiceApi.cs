using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NodaTime;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Common.Infrastructure.Abstractions;

public interface IAccountsServiceApiReadonly : IHostedService
{
    Task SubscribeToAccountState(int accountId, string accountServiceName, bool waitForInitialSnapshot,
        int timeoutMilliseconds = 10000, bool controlSequence = false);
    Task SubscribeToBrokerAccountState(int accountId, string accountServiceName, bool waitForInitialSnapshot,
        int timeoutMilliseconds = 10000, bool controlSequence = false);
    Task SusbscribeToStrategyState(int strategyId, bool waitForInitialSnapshot, int timeoutMilliseconds = 10000, bool controlSequence = false);
    
    Task<AccountStateReadonly?> RetrieveAccountStateAsync(int accountId, string accountsServiceName,
        int timeoutMilliseconds = 10000);
    Task<BrokerAccountStateReadonly?> RetrieveBrokerAccountStateAsync(int accountId, string accountsServiceName,
        int timeoutMilliseconds = 10000);
    
    
    void OnAccountSnapshot(AccountStateReadonly? snapshot, Guid requestId);
    void OnStrategySnapshot(StrategyStateReadonly? snapshot, Guid requestId);
}

public interface IAccountsServiceApi : IAccountsServiceApiReadonly
{
    void PlaceOrder(string accountServiceName, NewOrderSingle order);
    void CancelOrder(string accountServiceName, OrderCancelRequest request);
    void ReplaceOrder(string accountServiceName, OrderReplaceRequest request);
    void UpdateStrategyLastCalculationTs(int strategyId, Instant ts);
    void UpdateStrategyInternalState(int strategyId, string stateJson);
}

public interface IAccountsServiceBrokerAccountsApi : IAccountsServiceApiReadonly
{
    /// <summary>
    /// To be used when ES has a  
    /// </summary>
    void NotifyAccountOrdersNeedReconciliation(string accountServiceName, int accountId);
    void NotifyAccountNeedsReconciliation(string accountServiceName, int accountId);
}