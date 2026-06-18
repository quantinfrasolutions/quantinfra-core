using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.InProcess;
using QuantInfra.Common.Messaging.InProcess.Messages.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Connectors.Common;
using QuantInfra.Domain.Commands.Accounts.AccountsService;
using QuantInfra.Domain.Commands.Strategies.AccountsService;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Strategies.AccountsService;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading.Orders;
using DownstreamMessage = QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.DownstreamMessage;

namespace QuantInfra.Services.MonolithService.AccountsService;

public class AccountsServiceApiConfig
{
    public string AccountsServiceName { get; init; }
    public string ClientName { get; init; }
}

public class AccountsServiceApi : 
    Listener,
    IAccountsServiceApi,
    ITransport<DownstreamMessage>, 
    IMulticastListener
{
    private readonly Topology _topology;
    private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;
    private readonly ILogger<AccountsServiceApi> _logger;
    private readonly RequestsManager<Guid> _requestsManager = new(Guid.NewGuid);
    private readonly Dealer _dealer;

    public AccountsServiceApi(
        AccountsServiceApiConfig config,
        Topology topology,
        TransportFactory factory,
        Disruptor<IncomingDisruptorMessage> disruptor,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor,
        IDealerRouterMessageFactory messageFactory,
        IClock clock,
        ILogger<AccountsServiceApi> logger,
        ILoggerFactory loggerFactory
    ) : base(disruptor, clock)
    {
        _topology = topology;
        _outputDisruptor = outputDisruptor;
        _logger = logger;

        _dealer = factory.CreateDealer(config.AccountsServiceName, config.ClientName, disruptor);
        
        Sender = new Sender(_dealer, messageFactory, loggerFactory.CreateLogger<Sender>(), outputDisruptor, clock);
    }
    
    public Sender Sender { get; }
    
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        _topology.SubscribeToTopic(TopicDefinitions.HeartbeatsTopic, this);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
    }

    public void Subscribe(string? topicPrefix = null, bool controlSequence = false, string? sequenceResetTargetCompId = null)
    {
        throw new NotImplementedException();
    }

    public Task SubscribeToAccountState(int accountId, string accountServiceName, bool waitForInitialSnapshot, int timeoutMilliseconds = 10000, bool controlSequence = false)
    {
        _topology.SubscribeToTopic(TopicDefinitions.GetAccountUpdatesTopic(accountId), this);
        var task = RetrieveAccountStateAsync(accountId, accountServiceName, timeoutMilliseconds);
        return waitForInitialSnapshot ? task : Task.CompletedTask;
    }

    public Task SubscribeToBrokerAccountState(int accountId, string accountServiceName, bool waitForInitialSnapshot, int timeoutMilliseconds = 10000, bool controlSequence = false)
    {
        _topology.SubscribeToTopic(TopicDefinitions.GetAccountUpdatesTopic(accountId), this);
        var task = RetrieveBrokerAccountStateAsync(accountId, accountServiceName, timeoutMilliseconds);
        return waitForInitialSnapshot ? task : Task.CompletedTask;
    }

    public Task<AccountStateReadonly?> RetrieveAccountStateAsync(int accountId, string accountsServiceName, int timeoutMilliseconds = 10000)
    {
        var request = new GetAccountState(accountId, accountsServiceName, true);
        _logger.LogDebug("RetrieveAccountState, requestId={requestId}", request.RequestId);
        var task = _requestsManager.SendRequest<AccountStateReadonly?>(_ => Task.CompletedTask, id: request.RequestId, timeoutMilliseconds: timeoutMilliseconds);
        _outputDisruptor.PublishMessage(request);
        return task;
    }

    public Task<BrokerAccountStateReadonly?> RetrieveBrokerAccountStateAsync(int accountId, string accountsServiceName, int timeoutMilliseconds)
    {
        var request = new GetAccountState(accountId, accountsServiceName, true);
        _logger.LogDebug($"RetrieveBrokerAccountState, requestId={request.RequestId}");
        var task = _requestsManager.SendRequest<BrokerAccountStateReadonly?>(_ => Task.CompletedTask, id: request.RequestId, timeoutMilliseconds: timeoutMilliseconds);
        _outputDisruptor.PublishMessage(request);
        return task;
    }

    public Task SusbscribeToStrategyState(int strategyId, bool waitForInitialSnapshot, int timeoutMilliseconds = 10000, bool controlSequence = false)
    {
        _topology.SubscribeToTopic(TopicDefinitions.GetStrategyUpdatesTopic(strategyId), this);
        var task = RetrieveStrategyStateAsync(strategyId, timeoutMilliseconds);
        return waitForInitialSnapshot ? task : Task.CompletedTask;
    }
    
    public Task<StrategyStateReadonly?> RetrieveStrategyStateAsync(int strategyId, int timeoutMilliseconds = 10000)
    {
        var request = new GetStrategyState(strategyId, true);
        _logger.LogDebug("RetrieveStrategyState, requestId={requestId}", request.RequestId);
        var task = _requestsManager.SendRequest<StrategyStateReadonly?>(_ => Task.CompletedTask, id: request.RequestId, timeoutMilliseconds: timeoutMilliseconds);
        _outputDisruptor.PublishMessage(request);
        return task;
    }

    public void OnAccountSnapshot(AccountStateReadonly? snapshot, Guid requestId) => 
        Task.Run(() =>
        {
            _logger.LogDebug("Received account snapshot for requestId={requestId}", requestId);
            if (snapshot is BrokerAccountStateReadonly ba)
            {
                var request = _requestsManager.Requests.GetValueOrDefault(requestId);
                if (request != null && request is Request<Guid, BrokerAccountStateReadonly?>)
                {
                    _requestsManager.CompleteRequest(requestId, ba);
                    return;
                }
            }

            _requestsManager.CompleteRequest(requestId, snapshot);
        }).ConfigureAwait(false);

    public void OnStrategySnapshot(StrategyStateReadonly? snapshot, Guid requestId) =>
        Task.Run(() =>
        {
            _logger.LogDebug("Received strategy snapshot for requestId={requestId}", requestId);
            _requestsManager.CompleteRequest(requestId, snapshot);
        }).ConfigureAwait(false);

    public void PlaceOrder(string accountServiceName, NewOrderSingle order) =>
        _outputDisruptor.PublishMessage(new NewOrderCmd(accountServiceName, order));

    public void CancelOrder(string accountServiceName, OrderCancelRequest request) =>
        _outputDisruptor.PublishMessage(new CancelOrderCmd(accountServiceName, request));

    public void ReplaceOrder(string accountServiceName, OrderReplaceRequest request) =>
        _outputDisruptor.PublishMessage(new ReplaceOrderCmd(accountServiceName, request));

    public void UpdateStrategyLastCalculationTs(int strategyId, Instant ts)
    {
        _outputDisruptor.PublishMessage(new UpdateLastCalculationTsCmd(strategyId, ts));
    }

    public void UpdateStrategyInternalState(int strategyId, string stateJson)
    {
        _outputDisruptor.PublishMessage(new UpdateStrategyInternalStateCmd(strategyId, stateJson));
    }

    public void SendMessage(DownstreamMessage message)
    {
        throw new NotImplementedException();
    }
}