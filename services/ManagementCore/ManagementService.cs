using ManagementCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Common.Interfaces.Api.Strategies;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Common.Trading.Infrastructure;
using QuantInfra.Connectors.Common;
using QuantInfra.Domain.Commands.Accounts.AccountsService;
using QuantInfra.Domain.Commands.StaticData;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Sdk.Trading.Positions;
using OrderCancelRequest = QuantInfra.Sdk.Trading.Orders.OrderCancelRequest;

namespace QuantInfra.Services.ManagementCore;

public class ManagementService(
    IAccountRecordsRepository accountsRepository,
    IStrategyRecordsRepository strategiesRepository,
    ITradingAccountsRepository tradingAccountsRepository,
    IPublisher publisher, 
    RequestsManager<NewOrderIdentifier> newOrderRequestsManager,
    RequestsManager<Guid> requestsManager,
    ILogger<ManagementService> logger,
    IClock clock
)
{
    public async Task<int> CreateAccountAsync(CreateAccountRequest request, int userId)
    {
        logger.LogInformation($"Creating account {request.Name}");
        var account = await accountsRepository.CreateAccountAsync(request, userId);
        
        publisher.PublishUnwrappedObject(new AccountCreatedEvt(account.AccountId /* HACK */, account.AccountId, 
            account, clock.GetCurrentInstant()));
        
        return account.AccountId;
    }

    public async Task CreateSubaccountAsync(CreateSubaccountRequest request, int userId)
    {
        logger.LogInformation($"Creating subaccount {request}");
        var account = await accountsRepository.GetAccountRecordAsync(request.AccountId);
        var sa = await accountsRepository.GetAccountRecordAsync(request.SubaccountId);

        switch (request.Classifier)
        {
            case SubaccountType.Broker:
                if (sa?.AccountType != AccountType.BrokerAccount) throw new InvalidOperationException("Broker subaccount must be attached to broker account");
                break;
            default:
                throw new InvalidOperationException($"Unsupported subaccount type {request.Classifier}");
        }
        
        var subaccount = await accountsRepository.CreateSubaccountAsync(
            new Subaccount(0, request.AccountId, request.SubaccountId, request.Classifier, sa!.BrokerId), 
            userId
        );
        
        publisher.PublishUnwrappedObject(new SubaccountAssignedEvt(request.AccountId /* HACK */, account!.AccountServiceName, request.AccountId,
            subaccount, clock.GetCurrentInstant()));
    }

    public async Task CreateTradingClientConfigAsync(TradingClientConfig config, int userId)
    {
        logger.LogInformation($"Creating trading client config for account {config.AccountId}");
        await tradingAccountsRepository.CreateTradingClientConfig(config);
        
        publisher.PublishUnwrappedObject(new TradingClientConfigurationChangedEvt(config.AccountId, config.AccountId, new(config) { TradingClientSecret = null }, clock.GetCurrentInstant()));
    }

    public async Task DeleteTradingClientConfigAsync(int accountId, int userId)
    {
        logger.LogInformation($"Deleting trading client config for account {accountId}");
        await tradingAccountsRepository.RemoveTradingClientConfig(accountId);
        
        publisher.PublishUnwrappedObject(new TradingClientConfigurationChangedEvt(accountId, accountId, null, clock.GetCurrentInstant()));
    }
    
    public async Task<int> CreateStrategyAsync(CreateStrategyRequest request, int userId)
    {
        logger.LogInformation($"Creating strategy {request.Name}");
        
        var (strategy, account) = await strategiesRepository.CreateStrategyAsync(request, userId);

        publisher.PublishUnwrappedObject(new AccountCreatedEvt(account.AccountId /* HACK */, account.AccountId, 
            account, clock.GetCurrentInstant()));

        if (account.AccountType == AccountType.VirtualAccount && request.Account.AddInitialInvestment)
        {
            await CreateBalanceOperationAsync(new()
            {
                AccountId = account.AccountId,
                Amount = 100000, // TODO
                AssetId = request.Account.CurrencyId,
                AffectsBalance = true,
                AffectsInvestment = true,
                AffectsShareCount = true,
                AffectsPnL = false,
                IsCorrection = false,
            }, userId);
        }
        
        publisher.PublishUnwrappedObject(new StrategyCreatedEvt(strategy.StrategyId /* HACK */, strategy.StrategyId, 
            strategy, account, clock.GetCurrentInstant()));
        
        return account.AccountId;
    }

    public async Task StartStrategyAsync(int strategyId, int userId)
    {
        logger.LogInformation("Starting strategy");
        
        var strategy = await strategiesRepository.GetStrategyRecordAsync(strategyId);
        if (strategy.Status != StrategyStatus.Stopped) throw new InvalidOperationException("Strategy is already started");
        
        await strategiesRepository.UpdateStrategyStatusAsync(strategyId, StrategyStatus.Running);
        // No need to send an event, because SS anyway needs restarting
    }

    public async Task<int> CreateBalanceOperationAsync(NewBalanceOperation bo, int userId,
        int timeoutMilliseconds = 10000)
    {
        logger.LogInformation($"Create balance operation {bo}");
        
        var account = await accountsRepository.GetAccountRecordAsync(bo.AccountId);
        if (account == null) throw new KeyNotFoundException($"Account {bo.AccountId} not found");
        
        // TODO: check the uniqueness of ExternalId
        
        return await requestsManager.SendRequest<int>(
            reqId => Task.Run(() =>
                publisher.PublishUnwrappedObject(new ProcessBalanceOperationCmd(account.AccountServiceName, bo, reqId))
            ),
            timeoutMilliseconds: timeoutMilliseconds
        );
    }

    public async Task<ExecutionReport> PlaceOrderAsync(NewOrderSingle order, int userId, int timeoutMilliseconds = 10000)
    {
        logger.LogInformation($"Placing order {order}");
        
        var account = await accountsRepository.GetAccountRecordAsync(order.AccountId);
        if (account == null) throw new KeyNotFoundException($"Account {order.AccountId} not found");

        if (string.IsNullOrEmpty(order.ClOrdId))
        {
            order = new(order) { ClOrdId = Guid.NewGuid().ToString() };
        }
        
        var id = new NewOrderIdentifier(order.AccountId, order.ClOrdId);

        return await newOrderRequestsManager.SendRequest<ExecutionReport>(
            _ => Task.Run(() => 
                publisher.PublishUnwrappedObject(new NewOrderCmd(account!.AccountServiceName, order))
            ),
            timeoutMilliseconds: timeoutMilliseconds,
            id: id
        );
    }

    public async Task<IReadOnlyCollection<OrderStatus>> GetOrdersAsync(int accountId)
    {
        logger.LogInformation($"Getting orders for account {accountId}");
        
        var account = await accountsRepository.GetAccountRecordAsync(accountId);
        if (account == null) throw new KeyNotFoundException($"Account {accountId} not found");

        return await requestsManager.SendRequest<IReadOnlyCollection<OrderStatus>>(
            reqId => Task.Run(() => publisher.PublishUnwrappedObject(new GetActiveOrders(reqId, accountId, account.AccountServiceName)))
        );
    }

    public async Task<ExecutionReport> CancelOrderAsync(OrderCancelRequest request, int userId, int timeoutMilliseconds = 10000)
    {
        logger.LogInformation($"Canceling order {request}");

        Guid requestId;
        if (string.IsNullOrEmpty(request.ClOrdId) || !Guid.TryParse(request.ClOrdId, out requestId))
        {
            requestId = Guid.NewGuid();
            request = new(request) { ClOrdId = requestId.ToString() };
        }
        
        var account = await accountsRepository.GetAccountRecordAsync(request.AccountId);
        if (account == null) throw new KeyNotFoundException($"Account {request.AccountId} not found");

        return await requestsManager.SendRequest<ExecutionReport>(
            _ => Task.Run(() => 
                publisher.PublishUnwrappedObject(new CancelOrderCmd(account!.AccountServiceName, request, requestId))
            ),
            timeoutMilliseconds: timeoutMilliseconds,
            id: requestId
        );
    }
    
    public async Task<ExecutionReport> ReplaceOrderAsync(OrderReplaceRequest request, int userId, int timeoutMilliseconds = 10000)
    {
        logger.LogInformation($"Replacing order {request}");

        Guid requestId;
        if (string.IsNullOrEmpty(request.RequestId) || !Guid.TryParse(request.RequestId, out requestId))
        {
            requestId = Guid.NewGuid();
            request = new(request) { RequestId = requestId.ToString() };
        }
        
        var account = await accountsRepository.GetAccountRecordAsync(request.AccountId);
        if (account == null) throw new KeyNotFoundException($"Account {request.AccountId} not found");

        return await requestsManager.SendRequest<ExecutionReport>(
            _ => Task.Run(() => 
                publisher.PublishUnwrappedObject(new ReplaceOrderCmd(account!.AccountServiceName, request, requestId))
            ),
            timeoutMilliseconds: timeoutMilliseconds,
            id: requestId
        );
    }

    public Task RunEndOfDay(string accountServiceName, Instant referenceDt, int userId)
    {
        publisher.PublishUnwrappedObject(new RunEndOfDayCmd(accountServiceName, referenceDt));
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<Position>> GetPositionsAsync(int accountId, bool markToMarket)
    {
        logger.LogInformation($"Getting positions for account {accountId}");
        
        var account = await accountsRepository.GetAccountRecordAsync(accountId);
        if (account == null) throw new KeyNotFoundException($"Account {accountId} not found");

        return await requestsManager.SendRequest<IReadOnlyCollection<Position>>(
            reqId => Task.Run(() => publisher.PublishUnwrappedObject(new GetPositions(reqId, accountId, account.AccountServiceName, markToMarket)))
        );
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetBalancesAsync(int accountId)
    {
        logger.LogInformation($"Getting balances for account {accountId}");
        
        var account = await accountsRepository.GetAccountRecordAsync(accountId);
        if (account == null) throw new KeyNotFoundException($"Account {accountId} not found");
        
        return await requestsManager.SendRequest<IReadOnlyDictionary<int, decimal>>(
            reqId => Task.Run(() => publisher.PublishUnwrappedObject(new GetBalances(reqId, accountId, account.AccountServiceName)))
        );
    }

    public async Task<BrokerAccountReconciliationStatus?> GetBrokerAccountReconciliationStatusAsync(int accountId)
    {
        logger.LogInformation($"Getting account reconciliation status for account {accountId}");
        
        var account = await accountsRepository.GetAccountRecordAsync(accountId);
        if (account == null) throw new KeyNotFoundException($"Account {accountId} not found");
        if (account.AccountType != AccountType.BrokerAccount) throw new InvalidOperationException($"Account {accountId} is not BrokerAccount");

        return await requestsManager.SendRequest<BrokerAccountReconciliationStatus?>(
            reqId => Task.Run(() => publisher.PublishUnwrappedObject(new GetBrokerAccountReconciliationStatus(reqId, accountId, account.AccountServiceName)))
        );
    }

    public async Task Reconcile(int accountId)
    {
        logger.LogInformation($"Getting account reconciliation status for account {accountId}");
        
        var account = await accountsRepository.GetAccountRecordAsync(accountId);
        if (account == null) throw new KeyNotFoundException($"Account {accountId} not found");
        if (account.AccountType != AccountType.BrokerAccount) throw new InvalidOperationException($"Account {accountId} is not BrokerAccount");

        await requestsManager.SendRequest<AccountReconciliationStatusChangedEvt>(
            reqId => Task.Run(() => 
                publisher.PublishUnwrappedObject(new ClearBrokerAccountReconciliationStatus(account.AccountServiceName, accountId, clock.GetCurrentInstant(), reqId))
            )
        );
    }

    public Task ClearStaticDataCache(string accountServiceName, int userId)
    {
        publisher.PublishUnwrappedObject(ClearStaticDataCacheCmd.Create(accountServiceName));
        return Task.CompletedTask;
    }
}