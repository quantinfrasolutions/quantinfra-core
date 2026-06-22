using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using AccountsCore;
using Common.Infrastructure.Abstractions;
using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.BPL;
using QuantInfra.Common.ServiceBase.WAL;
using QuantInfra.Domain.AccountRecordsStateManager;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Strategies.AccountsService;
using QuantInfra.Domain.StrategyRecordsStateManager;
using QuantInfra.Domain.VirtualExecution;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Services.AccountsCore.EventHandlers;
using QuantInfra.Services.AccountsCore.State;

[assembly:InternalsVisibleTo("QuantInfra.Services.AccountsCore.Tests")]
namespace QuantInfra.Services.AccountsCore;

public class Bpl : BusinessLogicProcessorBase<AccountServiceState>
{
    private readonly Config _config;
    private readonly AccountServiceState _state;
    private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;
    private readonly StateManager _stateManager;
    private readonly AccountsServiceStateManager _strategyRecordsManager;
    private readonly IManagementNotificationsClient _client;
    private readonly ReceiverFilter _receiverFilter;
    private readonly VirtualExecutor _ve;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEventBus _eventBus;
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;
    private readonly DisruptorAsyncQueryBus _responseBus;
    private readonly ManagementEvents _mgmtHandler;
    private readonly List<IConfigurableLoggingModule> _loggingModules;
    private readonly string _serviceName;

    public Bpl(
        Config config,
        WalManager<AccountServiceState> walManager,
        // DownstreamFilter filter,
        // Finalizer finalizer,
        AccountServiceState state,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor,
        StateManager stateManager,
        AccountsServiceStateManager strategyRecordsManager,
        IManagementNotificationsClient client,
        ReceiverFilter receiverFilter,
        VirtualExecutor ve,
        ReplayingClock clock,
        ILoggerFactory loggerFactory,
        IEventBus eventBus,
        ICommandBus commandBus,
        IQueryBus queryBus,
        DisruptorAsyncQueryBus responseBus,
        IEnumerable<IConfigurableLoggingModule> loggingModules,
        ManagementEvents mgmtHandler
    ) : base(
        new()
        {
            ServiceName = config.AccountServiceName, 
            SingleHost = config.SingleHost,
            Monolith = config.Monolith,
            WritePerformanceMetrics = config.WritePerformanceMetrics,
            ProcessingDelayParams = config.ProcessingDelayParams,
            ProcessingTimeParams = config.ProcessingTimeParams,
            ReceiveMessageHopHistParams = config.ReceiveMessageHopHistParams,
            BplDelayParams = config.BplDelayParams,
            BplTimeParams = config.BplTimeParams,
            StateTimeParams = config.StateTimeParams,
        },
        walManager, state, outputDisruptor, clock, loggerFactory.CreateLogger<Bpl>())
    {
        _serviceName = config.AccountServiceName;
        _config = config;
        _state = state;
        _outputDisruptor = outputDisruptor;
        _stateManager = stateManager;
        _strategyRecordsManager = strategyRecordsManager;
        _client = client;
        _receiverFilter = receiverFilter;
        _ve = ve;
        _loggerFactory = loggerFactory;
        _eventBus = eventBus;
        _commandBus = commandBus;
        _queryBus = queryBus;
        _responseBus = responseBus;
        _mgmtHandler = mgmtHandler;
        _loggingModules = loggingModules.ToList();
    }
    
    public SemaphoreSlim StateInitialized { get; } = new(0);
    public SemaphoreSlim AccountsAndStrategiesReconciled { get; } = new(0);

    protected override void OnBeforeReplayingWal()
    {
        foreach (var acc in _state.UninitializedAccountStates)
        {
            _mgmtHandler.InstantiateAccountState(acc, _state.AccountRecords[acc.AccountId]);
        }

        foreach (var acc in _state.UninitializedBrokerAccountStates)
        {
            _mgmtHandler.InstantiateAccountState(acc, _state.AccountRecords[acc.AccountId]);
        }
        
        // foreach (var acc in _state.UninitializedEsaStates)
        // {
        //     _mgmtHandler.InstantiateAccountState(acc, _state.AccountRecords[acc.AccountId]);
        // }

        foreach (var s in _state.UninitializedStrategyStates)
        {
            _mgmtHandler.InstantiateStrategyState(s);
        }
        
        _ve.Initialize(_queryBus);

        if (_config.DisableLoggingOnReplay)
        {
            foreach (var m in _loggingModules) m.DisableLogging();
        }
    }

    protected override void OnStateInitialized() => OnStateInitializedInternal();

    internal void OnStateInitializedInternal()
    {
        _receiverFilter.InitializeState();

        foreach (var m in _loggingModules) m.EnableLogging();
        StateInitialized.Release();
    }

    protected override void OnBeforeHandleMessage(ITransportMessage msg)
    {
        _receiverFilter.UpdateState(msg.SenderCompId, msg.SessionId, msg.SequenceNumber);
    }

    protected override void OnReconciliationDone() => AccountsAndStrategiesReconciled.Release();

    internal void SendAccountSnapshots()
    {
        var existingStates = State.AccountStates.Keys.ToHashSet();
        
        Logger.LogInformation($"Sending snapshots for {existingStates.Count} accounts");
        foreach (var aid in existingStates)
        {
            _responseBus.HandleAnonymousAsyncQuery(new GetAccountState(aid, _serviceName, true));
        }
        
        Logger.LogInformation($"Accounts reconciled");
    }

    internal void ReconcileStrategyStates(Instant processingDt)
    {
        var existingStates = State.StrategyStates.Keys.ToHashSet();
        
        Logger.LogInformation($"Sending snapshots for {existingStates.Count} strategies");
        foreach (var sid in existingStates)
        {
            _responseBus.HandleAnonymousAsyncQuery(new GetStrategyState(sid, true));
        }
        
        Logger.LogInformation($"Accounts reconciled");
        // var existingStates = State.StrategyStates.Keys.ToHashSet();
        //
        // Logger.LogInformation($"Sending snapshots for {existingStates.Count} strategies");
        // foreach (var sid in existingStates)
        // {
        //     _responseBus.HandleAnonymousAsyncQuery(new GetStrategyState(sid, true));
        // }
        //
        //
        // Logger.LogInformation($"Adding new strategies");
        // // TODO: move to RecordsManager
        // var newStrategies = _strategyRecordsManager.StrategyRecords.Where(kv => !existingStates.Contains(kv.Key))
        //     .ToList();
        //
        // foreach (var s in newStrategies)
        // {
        //     Logger.LogInformation($"Adding missing strategy {s.Key} {s.Value.Name}");
        //     var account = _stateManager.AccountRecords[s.Value.AccountId];
        //     var evt = new StrategyCreatedEvt(s.Key, s.Key, s.Value, account, processingDt);
        //     // HACK. Here, we emulate the Management Service sending the event to the AS/OMS
        //     _client.PublishMessage(evt, processingDt);
        // }
        
        
        Logger.LogInformation($"Strategies reconciled");
    }

    protected override void HandleMessage(object message, bool isReplay, Instant processingDt, long swReceivedAt) => 
        Handle(message, isReplay, processingDt, swReceivedAt);

    internal void Handle(object message, bool isReplay, Instant processingDt, long swReceivedAt)
    {
        switch (message)
        {
            case IEvent e:
                // Mgmt service sends Account/StrategyId as the event id. Here, we populate the correct event id
                int? accountId = null, strategyId = null;
                if (e is AccountCreatedEvt ac)
                {
                    e = ac with { EventId = State.GetNextEventId(), Timestamp = processingDt };
                    accountId = ac.AccountId;
                }

                if (e is TradingClientConfigurationChangedEvt tc)
                {
                    e = tc with { EventId = State.GetNextEventId(), Timestamp = processingDt };
                    accountId = tc.AccountId;
                }
                if (e is StrategyCreatedEvt sc)
                {
                    e = sc with { EventId = State.GetNextEventId(), Timestamp = processingDt };
                    strategyId = sc.StrategyId;
                }
                if (e is SubaccountAssignedEvt sa)
                {
                    e = sa with { EventId = State.GetNextEventId(), Timestamp = processingDt };
                }
                
                _eventBus.ApplyAnonymousExternalEvent(e);
                _eventBus.EmitAnonymousEvent(e);
                
                if (!isReplay)
                {
                    if (accountId.HasValue) _responseBus.HandleAsyncQuery<GetAccountState, AccountStateReadonly?>(new(accountId.Value, _serviceName));
                    if (strategyId.HasValue) _responseBus.HandleAsyncQuery<GetStrategyState, StrategyStateReadonly?>(new(strategyId.Value));
                }
                
                break;
            case ICommand c:
                _commandBus.SendAnonymousCommand(c);
                break;
            case GetAccountState gs:
                if (isReplay) return;
                if (gs.AccountServiceName != _serviceName) return;
                Logger.LogInformation("GetAccountState, accountId={accountId}, requestId={requestId}, useMulticast={useMulticast}", 
                    gs.AccountId, gs.RequestId, gs.UseMulticast);
                var account = State.AccountRecords.GetValueOrDefault(gs.AccountId);
                if (account is null)
                {
                    Logger.LogWarning("Could not get state for account {accountId}, requestId={requestId}", gs.AccountId, gs.RequestId);
                    _responseBus.SendAsyncQueryResponse<GetAccountState, AccountStateReadonly?>(new(gs.RequestId, null, gs.UseMulticast));
                }
                else
                {
                    switch (account.AccountType)
                    {
                        case AccountType.VirtualAccount or AccountType.StrategySubAccount:
                            _responseBus.HandleAnonymousAsyncQuery(gs);
                            break;
                        case AccountType.BrokerAccount:
                            _responseBus.HandleAnonymousAsyncQuery(new GetBrokerAccountState(gs.RequestId, _serviceName, gs.AccountId, gs.UseMulticast));
                            break;
                    }
                }
                break;
            case IAsyncQuery q:
                if (!isReplay) _responseBus.HandleAnonymousAsyncQuery(q);
                break;
            case AsyncQueryResponse r:
                _eventBus.HandleAnonymousAsyncQueryResult(r);
                break;
        }
        _outputDisruptor.PublishMessage(new SyncMessage(), swReceivedAt: swReceivedAt);
    }
}