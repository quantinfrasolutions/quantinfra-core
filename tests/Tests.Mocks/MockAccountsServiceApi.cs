using NodaTime;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Accounts.ExternalAccounts;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Tests.Mocks;

public class MockAccountsServiceApi : IAccountsServiceApi
{
    public List<StateSubscriptionRequest> AccountStateSubscriptions { get; } = new();
    public List<StateSubscriptionRequest> StrategyStateSubscriptions { get; } = new();
    public List<StateSubscriptionRequest> BrokerAccountStateSubscriptions { get; } = new();
    public List<NewOrderSingle> Orders { get; } = new();
    
    public async Task SubscribeToAccountState(int accountId, bool waitForInitialSnapshot)
    {
        var subscription = new StateSubscriptionRequest(accountId, waitForInitialSnapshot, new TaskCompletionSource<int>());
        AccountStateSubscriptions.Add(subscription);
        if (waitForInitialSnapshot) await subscription.Tcs.Task;
    }

    public async Task SubscribeToBrokerAccountState(int accountId, bool waitForInitialSnapshot)
    {
        var subscription = new StateSubscriptionRequest(accountId, waitForInitialSnapshot, new TaskCompletionSource<int>());
        BrokerAccountStateSubscriptions.Add(subscription);
        if (waitForInitialSnapshot) await subscription.Tcs.Task;
    }

    public Task SubscribeToAccountState(int accountId, string accountServiceName, bool waitForInitialSnapshot,
        int timeoutMilliseconds = 10000, bool controlSequence = false)
    {
        AccountStateSubscriptions.Add(new StateSubscriptionRequest(accountId, waitForInitialSnapshot, new TaskCompletionSource<int>()));
        return Task.CompletedTask;
    }

    public Task SubscribeToBrokerAccountState(int accountId, string accountServiceName, bool waitForInitialSnapshot,
        int timeoutMilliseconds = 10000, bool controlSequence = false)
    {
        throw new NotImplementedException();
    }

    public Task SusbscribeToStrategyState(int strategyId, bool waitForInitialSnapshot, int timeoutMilliseconds = 10000,
        bool controlSequence = false)
    {
        StrategyStateSubscriptions.Add(new StateSubscriptionRequest(strategyId, waitForInitialSnapshot, new TaskCompletionSource<int>()));
        return Task.CompletedTask;
    }

    public Task<AccountStateReadonly?> RetrieveAccountStateAsync(int accountId, string accountsServiceName,
        int timeoutMilliseconds)
    {
        throw new NotImplementedException();
    }

    public Task<BrokerAccountStateReadonly?> RetrieveBrokerAccountStateAsync(int accountId, string accountsServiceName,
        int timeoutMilliseconds)
    {
        throw new NotImplementedException();
    }

    public async Task SusbscribeToStrategyState(int strategyId, bool waitForInitialSnapshot)
    {
        var subscription = new StateSubscriptionRequest(strategyId, waitForInitialSnapshot, new TaskCompletionSource<int>());
        StrategyStateSubscriptions.Add(subscription);
        if (waitForInitialSnapshot) await subscription.Tcs.Task;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void PlaceOrder(string accountServiceName, NewOrderSingle order) => Orders.Add(order);
    public void CancelOrder(string accountServiceName, OrderCancelRequest request)
    {
        throw new NotImplementedException();
    }

    public void CancelOrder(int accountId, long orderId)
    {
        throw new NotImplementedException();
    }

    public void ReplaceOrder(string accountServiceName, OrderReplaceRequest request)
    {
        throw new NotImplementedException();
    }

    public void UpdateStrategyLastCalculationTs(int strategyId, Instant ts)
    {
        throw new NotImplementedException();
    }

    public void UpdateStrategyInternalState(int strategyId, string stateJson)
    {
        throw new NotImplementedException();
    }

    public void OnAccountSnapshot(AccountStateReadonly? snapshot, Guid requestId)
    {
        throw new NotImplementedException();
    }

    public void OnStrategySnapshot(StrategyStateReadonly? snapshot, Guid requestId)
    {
        throw new NotImplementedException();
    }

    public void ProcessExternalExecutionReport(ExternalExecutionReport er)
    {
        throw new NotImplementedException();
    }

    public void ProcessExternalOrderCancelReject(ExternalOrderCancelReject ocr)
    {
        throw new NotImplementedException();
    }

    public void ProcessExternalTrade(ExternalTradeRecord trade)
    {
        throw new NotImplementedException();
    }

    public void ProcessExternalAccountFullSnapshot(ExternalAccountFullSnapshot snapshot)
    {
        throw new NotImplementedException();
    }
}

public class StateSubscriptionRequest
{
    public StateSubscriptionRequest(int entityId, bool waitForInitialSnapshot, TaskCompletionSource<int> tcs)
    {
        EntityId = entityId;
        WaitForInitialSnapshot = waitForInitialSnapshot;
        Tcs = tcs;
    }

    public int EntityId { get; }
    public bool WaitForInitialSnapshot { get; }
    public TaskCompletionSource<int> Tcs { get; }
    
    public void Complete() => Tcs.SetResult(EntityId);
}