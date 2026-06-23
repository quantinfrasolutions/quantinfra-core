using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.Handlers;
using QuantInfra.Common.ServiceBase.ServiceMessages;
using QuantInfra.Common.ServiceBase.WAL;
using QuantInfra.Domain.AccountRecordsStateManager;
using QuantInfra.Domain.StrategyRecordsStateManager;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Services.AccountsCore.EventHandlers;
using QuantInfra.Services.AccountsCore.Jobs;
using QuantInfra.Services.AccountsCore.State;
using Quartz;

namespace QuantInfra.Services.AccountsCore;

public class AccountsService : IHostedService
{
    private readonly Config _config;
    private readonly StateManager _accountRecordsStateManager;
    private readonly AccountsServiceStateManager _strategyRecordsManager;
    private readonly MulticastSender _sender;
    private readonly Disruptor<IncomingDisruptorMessage> _inputDisruptor;
    private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;
    private readonly Bpl _bpl;
    private readonly ILoggerFactory _loggerFactory;
    private readonly WalManager<AccountServiceState> _walManager;
    private readonly ILogger _logger;
    private readonly IMarketDataClient _mdClient;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly Persister _persister;
    private readonly IPersistentEventStorage<AccountServiceState> _persistentEventStorage;
    private readonly AccountServiceState _state;
    private readonly ReplayingClock _clock;
    private readonly ManagementEvents _managementEvents;
    private readonly List<IIncomingTransport> _incomingTransports;

    public AccountsService(
        Config config,
        IComponentExceptionHandler exceptionHandler,
        WalManager<AccountServiceState> walManager,
        Serializer serializer,
        Parser parser,
        StateManager accountRecordsStateManager,
        AccountsServiceStateManager strategyRecordsManager,
        MulticastSender sender,
        Disruptor<IncomingDisruptorMessage> inputDisruptor,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor,
        Bpl bpl,
        ILoggerFactory loggerFactory, 
        IMarketDataClient mdClient, 
        ISchedulerFactory schedulerFactory,
        Persister persister,
        IEnumerable<IIncomingTransport> incomingTransports,
        IPersistentEventStorage<AccountServiceState> persistentEventStorage,
        AccountServiceState state,
        ReplayingClock clock,
        ManagementEvents managementEvents
    )
    {
        _logger = loggerFactory.CreateLogger<AccountsService>();
        _config = config;
        _walManager = walManager;
        _accountRecordsStateManager = accountRecordsStateManager;
        _strategyRecordsManager = strategyRecordsManager;
        _sender = sender;
        _inputDisruptor = inputDisruptor;
        _outputDisruptor = outputDisruptor;
        _bpl = bpl;
        _loggerFactory = loggerFactory;
        _mdClient = mdClient;
        _schedulerFactory = schedulerFactory;
        _persister = persister;
        _persistentEventStorage = persistentEventStorage;
        _state = state;
        _clock = clock;
        _managementEvents = managementEvents;
        _incomingTransports = incomingTransports.ToList();

        if (_walManager.WalRollPeriodEvents > _inputDisruptor.BufferSize) throw new InvalidOperationException("Wal roll period should be less than input disruptor buffer size");
        // TODO: the same for the output disruptor
        
        if (config.Monolith)
        {
            _inputDisruptor.HandleEventsWith(serializer).Then(_walManager).Then(bpl);
        }
        else
        {
            _inputDisruptor.HandleEventsWith(_walManager, parser).Then(bpl);
        }
        
        // Even in case of the monolith app, if AS fails, let the whole application fail
        _inputDisruptor.SetDefaultExceptionHandler(new DisruptorExceptionHandler<IncomingDisruptorMessage>(
            exceptionHandler, loggerFactory.CreateLogger<DisruptorExceptionHandler<IncomingDisruptorMessage>>()));
        var handlers = _outputDisruptor.HandleEventsWith(_sender);
        if (config.PersistEventsAndProjections) handlers.Then(_persister);
        _outputDisruptor.SetDefaultExceptionHandler(new DisruptorExceptionHandler<OutgoingDisruptorMessage>(
            exceptionHandler, loggerFactory.CreateLogger<DisruptorExceptionHandler<OutgoingDisruptorMessage>>()));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting service");
        
        _inputDisruptor.Start();
        
        await _persister.InitializeAsync();
        var lastPersistedEventId = _persister.LastPersistedEventId;
        _logger.LogInformation($"Persister initialized, lastPersistedEventId={lastPersistedEventId}");
        _sender.UpdateLastSentEventId(lastPersistedEventId);

        try
        {
            _walManager.Start();
        }
        catch (WalDirectoryEmptyException)
        {
            _logger.LogWarning("WAL directory is empty, trying to recover the state from the storage");
            var remoteState = await _persistentEventStorage.GetLatestStateSnapshot(_config.AccountServiceName);
            if (remoteState is not null)
            {
                _state.Initialize(remoteState);
            }
            
            var hydration = new Hydrator(_state, _loggerFactory);
            
            _logger.LogInformation("Snapshot recovered from the storage, hydrating");
            var offset = 0;
            var limit = _config.HydrationBatchSize;
            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Processing batch {offset}-{offset + limit}");
                var batch = await _persistentEventStorage.GetEventsSinceLastSnapshot(_config.AccountServiceName, limit, offset);
                if (batch.Count == 0) break;
                hydration.ProcessBatch(batch);
                offset += limit;
            }
            _logger.LogInformation("State restored from the storage");
            var partition = _clock.AbsoluteClock.GetCurrentInstant().ToUnixTimeMilliseconds();
            _walManager.PersistState(_state, partition, true);
            _logger.LogInformation("Finished initializing the state from the persistent storage, restaring the service");
            Environment.Exit(0);
        }
        
        await _bpl.StateInitialized.WaitAsync(cancellationToken);
        _logger.LogInformation("State initialized");
        
        await _accountRecordsStateManager.LoadAccountRecordsAsync(_clock.AbsoluteClock.GetCurrentInstant());
        await _strategyRecordsManager.LoadStrategiesRecordsAsync(_clock.AbsoluteClock.GetCurrentInstant());
        _bpl.SendAccountSnapshots();
        _bpl.ReconcileStrategyStates(_clock.AbsoluteClock.GetCurrentInstant());
        _walManager.FinishReconciliation();
        
        await _bpl.AccountsAndStrategiesReconciled.WaitAsync(cancellationToken);
        _managementEvents.EnableRealtime();
        _logger.LogInformation("Accounts and strategies reconciled");
        
        _outputDisruptor.Start();
        _logger.LogInformation("OutputDisruptor started");
        
        await Task.WhenAll(_incomingTransports.Select(t => t.StartAsync(cancellationToken)));
        _logger.LogInformation("Incoming transports started");
        
        await _mdClient.StartAsync(cancellationToken);
        await _mdClient.SubscribeToLastContractPricesAsync();
        _logger.LogInformation("Market data client started");
        
        var brokerAccounts = _state.AccountRecords.Values
            .Where(a => a.AccountType == AccountType.BrokerAccount && a.TradingClientConfig is not null)
            .ToList();
        _logger.LogInformation($"Subscribing to broker accounts [{string.Join(", ", brokerAccounts.Select(ba => ba.AccountId))}]");
        
        await ConfigureJobs();
        _logger.LogInformation("Jobs started");
        
        _logger.LogInformation("Service started");
    }

    private async Task ConfigureJobs()
    {
        var scheduler = await _schedulerFactory.GetScheduler();

        var heartbeatJobKey = new JobKey("heartbeat");
        if (await scheduler.CheckExists(heartbeatJobKey))
        {
            await scheduler.DeleteJob(heartbeatJobKey);
        }
        
        await scheduler.ScheduleJob(
            JobBuilder.Create<HeartbeatJob>()
                .WithIdentity(heartbeatJobKey)
                .SetJobData(new JobDataMap())
                .Build(), 
            TriggerBuilder.Create()
                .WithIdentity("heartbeatTrigger")
                .StartNow()
                .WithSimpleSchedule(s => s.WithInterval(_config.HeartbeatInterval.ToTimeSpan()).RepeatForever())
                .Build()
        );
        
        
        var eodExecutionTime = _config.MtmUtcOffset.Plus(_config.MtmJobDelay);

        var mtmJobKey = new JobKey("mtmJob");
        if (await scheduler.CheckExists(mtmJobKey))
        {
            await scheduler.DeleteJob(mtmJobKey);
        }

        var cronString =
            $"{eodExecutionTime.Seconds} {eodExecutionTime.Minutes} {eodExecutionTime.Hours} 1/1 * ? *";
        _logger.LogInformation($"Scheduling MTM job with CRON string {cronString}");
        await scheduler.ScheduleJob(
            JobBuilder.Create<EndOfDayJob>()
                .WithIdentity(mtmJobKey)
                .SetJobData(new JobDataMap())
                .Build(), 
            TriggerBuilder.Create()
                .WithIdentity("mtmTrigger")
                .StartNow()
                .WithCronSchedule(cronString)
                .Build()
        );
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping service");
        await Task.WhenAll(_incomingTransports.Select(t => t.StopAsync(cancellationToken)));
        
        _inputDisruptor.PublishParsedMessage(new StopEvt(), 0);
        if (_config.PersistEventsAndProjections) await _persister.StopSemaphore.WaitAsync(cancellationToken);
        else await _sender.StopSemaphore.WaitAsync(cancellationToken);
        
        _inputDisruptor.Halt();
        _outputDisruptor.Halt();
        
        await _mdClient.StopAsync(cancellationToken);
        
        _logger.LogInformation("Service stopped");
    }
}