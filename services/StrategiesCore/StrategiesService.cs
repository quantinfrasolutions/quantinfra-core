using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Common.Metrics;
using Disruptor.Dsl;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.Metrics;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.Handlers;
using QuantInfra.Domain.HostedStrategies;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Domain.StrategyRecordsStateManager;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.Strategies;
using StrategiesCore;

[assembly:InternalsVisibleTo("QuantInfra.Services.StrategiesCore.Tests")]
[assembly:InternalsVisibleTo("Tests.v6.E2E")]

namespace QuantInfra.Services.StrategiesCore
{
    public sealed class StrategiesService : IHostedService
    {
        private readonly Config _config;
        private readonly StrategiesServiceStateManager _strategyRecordsStrategiesServiceStateManager;
        private readonly HostedStrategiesRunner _runner;
        private readonly IManagementNotificationsClient _managementNotificationsClient;
        private readonly Disruptor<IncomingDisruptorMessage> _inputDisruptor;
        private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;
        private readonly ILogger _logger;
        private readonly IAccountsServiceApi _accountsServiceApi;
        private readonly IClock _clock;
        private readonly IMarketDataHistoryProvider _mdHistoryProvider;
        private readonly IMarketDataClient _mdClient;
        private readonly Sender _sender;
        private readonly IQueryBus _queryBus;
        private readonly List<IIncomingTransport> _incomingTransports;

        public StrategiesService(
            Config config,
            IComponentExceptionHandler exceptionHandler,
            Parser parser,
            StrategiesServiceStateManager strategyRecordsStrategiesServiceStateManager,
            HostedStrategiesRunner runner,
            IManagementNotificationsClient managementNotificationsClient,
            Disruptor<IncomingDisruptorMessage> inputDisruptor,
            Disruptor<OutgoingDisruptorMessage> outputDisruptor,
            Bpl bpl,
            ILoggerFactory loggerFactory, 
            IAccountsServiceApi accountsServiceApi, 
            IClock clock, 
            IMarketDataHistoryProvider mdHistoryProvider, 
            IMarketDataClient mdClient,
            Sender sender,
            IEnumerable<IIncomingTransport> incomingTransports,
            IQueryBus queryBus
        )
        {
            _logger = loggerFactory.CreateLogger<StrategiesService>();
            _config = config;
            _strategyRecordsStrategiesServiceStateManager = strategyRecordsStrategiesServiceStateManager;
            _runner = runner;
            _managementNotificationsClient = managementNotificationsClient;
            _inputDisruptor = inputDisruptor;
            _outputDisruptor = outputDisruptor;
            _accountsServiceApi = accountsServiceApi;
            _clock = clock;
            _mdHistoryProvider = mdHistoryProvider;
            _mdClient = mdClient;
            _sender = sender;
            _queryBus = queryBus;
            _incomingTransports = incomingTransports.ToList();

            // _inputDisruptor.ConfigureDisruptor(
            //     new(parser, 1),
            //     new(bpl, _config.UseSingleThreadForInputDisruptor ? 1 : 2)
            // );
            // _inputDisruptor.HandleEventsWith(parser).Then(bpl);
            if (config.Monolith) _inputDisruptor.HandleEventsWith(bpl);
            else _inputDisruptor.HandleEventsWith(parser).Then(bpl);

            _inputDisruptor.SetDefaultExceptionHandler(new DisruptorExceptionHandler<IncomingDisruptorMessage>(
                exceptionHandler, loggerFactory.CreateLogger<DisruptorExceptionHandler<IncomingDisruptorMessage>>()));
            _outputDisruptor.HandleEventsWith(sender);
            _outputDisruptor.SetDefaultExceptionHandler(new DisruptorExceptionHandler<OutgoingDisruptorMessage>(
                exceptionHandler, loggerFactory.CreateLogger<DisruptorExceptionHandler<OutgoingDisruptorMessage>>()));

            if (config.WritePerformanceMetrics)
            {
                var sendingDelay = SharedMetricsDefinition.GetSendingDelay(config.StrategiesServiceName, config.Monolith,
                    config.SendingDelayParams[0], config.SendingDelayParams[1], config.SendingDelayParams[2]);
                _sender.OnBeforeHandle = data =>
                {
                    if (data.SwPublishedAt != 0)
                    {
                        sendingDelay!.Observe(MetricsUtils.GetUnixMicro() - data.SwPublishedAt);
                    }
                };
            }
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // 1. Request strategies managed by the service
            // 2. Subscribe to accounts and strategies state updates from AS/OMS and wait for the initial snapshots. Do not process strategy callbacks so far,
            //      because market data is not initialized yet
            // 3. Hydrate bar storages and subscribe to market data updates
            // 4. Deploy the strategies and start processing all market data and account events
            
            _logger.LogInformation("Starting service");
            
            _inputDisruptor.Start();
            _outputDisruptor.Start();
            _logger.LogInformation("Disruptors started");

            await Task.WhenAll(_incomingTransports.Select(t => t.StartAsync(cancellationToken)));
            _logger.LogInformation("Incoming transports started");
            
            await _accountsServiceApi.StartAsync(cancellationToken);
            _logger.LogInformation("AccountsServiceApi started");
            
            _sender.Start();
            _logger.LogInformation("Sender started");
            
            await _strategyRecordsStrategiesServiceStateManager.LoadStrategiesRecordsAsync(cancellationToken);
            var activeStrategies = _strategyRecordsStrategiesServiceStateManager.StrategyRecords.Values
                .Where(s => s.Status.IsActive()).ToList();
            
            _runner.Initialize(activeStrategies, _strategyRecordsStrategiesServiceStateManager.AccountRecords.Values.ToList());
            
            // var activeEsas =  _strategyRecordsStrategiesServiceStateManager.EsaSubscriptions.Values
            //     .Where(s => s.SubscriptionStatus.IsActive()).ToList();
            
            // _runner.Initialize(activeStrategies, activeEsas, _strategyRecordsStrategiesServiceStateManager.AccountRecords.Values.ToList());
            
            _logger.LogInformation($"Subscribing to strategies and account updates");

            // TODO: retrieve the state now and subscribe (with possible reconciliation) after loading market data
            // Otherwise, we may miss the processing of updates while market data is not yet retrieved and strategies are full functional
            
            var tasks = activeStrategies.SelectMany(s => new[]
            {
                Task.Run(() => _accountsServiceApi.SubscribeToAccountState(s.AccountId, _config.AccountsServiceName, true, _config.AccountServiceTimeout), cancellationToken),
                Task.Run(() => _accountsServiceApi.SusbscribeToStrategyState(s.StrategyId, true, _config.AccountServiceTimeout), cancellationToken)
            })
            //     .Union(activeEsas.SelectMany(s => new[]
            // {
            //     Task.Run(() => _accountsServiceApi.SubscribeToAccountState(s.ExecutableSubaccountId, _config.AccountsServiceName, true, _config.AccountServiceTimeout), cancellationToken),
            // }))
                ;
            var delay = Task.Run(async () => await Task.Delay(_config.AccountServiceTimeout));

            if (await Task.WhenAny(delay, Task.WhenAll(tasks)) == delay)
            {
                _logger.LogError("Timeout while subscribing to strategies and account updates");
                throw new TimeoutException("Timeout while subscribing to strategies and account updates");
            }
            _logger.LogInformation("Subscribed");
            
            _runner.LoadMarketData(_clock.GetCurrentInstant(), _mdHistoryProvider);
            
            await _mdClient.StartAsync(cancellationToken);

            var mdTasks = _runner.Aggregator.GetMarketDataSubscriptions()
                .Select(s => Task.Run(() => s.SubscriptionType switch
                {
                    SubscriptionType.Candles1M => _mdClient.SubsribeToCandles1M(s.StreamId!.Value),
                    _ => throw new NotImplementedException(),
                }, cancellationToken))
                .Union(_runner.Aggregator.GetBestBidOfferContractIds().Select(cid =>
                    Task.Run(() => _mdClient.SubscribeToBestBidOffers(cid))))
                .Union(_runner.Aggregator.GetOrderBookContractIds().Select(cid =>
                    Task.Run(() =>
                    {
                        var mdsName = _queryBus.Query<GetContractOrderBookSubscriptionServiceName, string?>(new(cid));
                        if (string.IsNullOrEmpty(mdsName)) throw new Exception($"No order book subscriptions configured for contract {cid}");
                        return _mdClient.SubscribeToOrderBook(cid, mdsName);
                    })));
            await Task.WhenAll(mdTasks);
            _logger.LogInformation("Subscribed to market data");
        
            await _managementNotificationsClient.StartAsync(cancellationToken);
            
            _logger.LogInformation("Calling Deploy");
            _runner.Deploy(_clock.GetCurrentInstant());
        
            _logger.LogInformation("Service started");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _accountsServiceApi.StopAsync(cancellationToken);
            await _mdClient.StopAsync(cancellationToken);
        }
    }
}