using Common.Utils.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Messaging.Patterns;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.Handlers;
using QuantInfra.Common.Trading.Infrastructure;
using QuantInfra.Domain.Accounts.AccountStateClientManager;
using QuantInfra.Domain.Accounts.AccountStateClientManager.Events;
using QuantInfra.Domain.Events.Accounts.AccountsService;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Accounts.External;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading.Infrastructure;
using QuantInfra.Services.ExecutionCore.EventHandlers;
using QuantInfra.Services.ExecutionCore.Queries;
using ExternalExecutionReportEvt = QuantInfra.Domain.Events.Accounts.External.ExternalExecutionReportEvt;

namespace QuantInfra.Services.ExecutionCore;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureExecutionService(this IServiceCollection services, IConfiguration configuration, 
        string sectionName = "execution-service",
        Action<Config>? configureAction = null
    ) => services
        .Configure<Config>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<Config>(sc =>
        {
            var config = sc.GetRequiredService<IOptions<Config>>().Value;
            configureAction?.Invoke(config);
            return config;
        });
    
    public static IServiceCollection AddExecutionService(this IServiceCollection services, bool addHostedService = true,
        IEnumerable<string>? tradingClientAdditionalAssemblyNames = null)
    {
        services
            .AddSingleton<ExecutionService>()
            
            .AddInputDisruptor()
            .AddOutputDisruptor()
            
            .AddDisruptorPublishingSubscriber()
            
            // .AddSingleton<ITypeResolver>(sp => new MultipleAssembliesTypeResolver([
            //     "Common.Messaging",
            //     "Common.EventSourcing",
            //     "Common.ServiceBase",
            //     "Domain.Events.Accounts.Management",
            //     "Domain.Events.Accounts.AccountsService",
            //     "Domain.Events.Strategies.Management",
            //     "Domain.Events.Strategies.AccountsService",
            //     "Domain.Queries.Accounts.AccountsService",
            //     "Domain.Commands.Accounts.AccountsService",
            //     "Domain.Queries.Accounts.ExecutionService",
            //     "QuantInfra.Domain.Commands.StaticData",
            // ]))
            
            .AddSingleton<ExecutionServiceState>()
            .AddSingleton<IQueryHandler<GetAccount, AccountRecordV6?>>(sp => sp.GetRequiredService<ExecutionServiceState>())
            
            .AddSingleton<Bpl>()
            
            // .AddAccountStateClientManager()
            .AddSingleton<StateManager>()
            .AddSingleton<AccountsStateManager>(sp => sp.GetRequiredService<StateManager>())
            .AddAccountStateClientManagerHandlers()
            
            .AddSingleton<HostedTradingClientsProvider>()
            .AddSingleton<IQueryHandler<GetTradingClient, IHostedTradingClient?>>(sp => sp.GetRequiredService<HostedTradingClientsProvider>())
            
            .AddDisruptorAsyncQueryBus()
            
            .AddKeyedSingleton<ITypeResolver>(
                "trading-clients-resolver", 
                (sp, _) =>
                {
                    List<string> assemblies =
                    [
                        "QuantInfra.Connectors.Binance.Futures.Usdm",
                        // "Ibkr.TwsApi.TradingClient",
                    ];
                    if (tradingClientAdditionalAssemblyNames != null) assemblies.AddRange(tradingClientAdditionalAssemblyNames);
                    return new MultipleAssembliesTypeResolver(assemblies);
                })
            .AddSingleton<ITradingClientResponsesHandler, DisruptorPublisher>()
            .AddSingleton<ITradingClientFactory>(sp => new TradingClientFactory(
                sp.GetRequiredService<IServiceProvider>(),
                sp.GetRequiredKeyedService<ITypeResolver>("trading-clients-resolver"),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IClock>(),
                sp.GetRequiredService<ITradingClientResponsesHandler>()
            ))
            
            .AddSingleton<ParserConfig>(sp =>
            {
                var config = sp.GetRequiredService<Config>();
                return new ParserConfig
                {
                    WritePerformanceMetrics = config.WritePerformanceMetrics,
                    ServiceName = config.ExecutionServiceName,
                    Monolith =  config.Monolith,
                };
            })
            .AddSingleton<Parser>()
            
            .AddSingleton<RequestSnapshotHandler>()
            .AddSingleton<IRequestSnapshotMessageHandler>(sp => sp.GetRequiredService<RequestSnapshotHandler>())
            
            .AddSingleton<ExternalTradingEventsHandler>()
            .AddSingleton<IEventHandler<NewOrderSingleExternalCreatedEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<OrderCancelRequestExternalCreatedEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<OrderReplaceRequestExternalCreatedEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<BrokerAccountNeedsOrdersReconciliationEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<BrokerAccountNeedsTradesReconciliationEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<AccountMissingVersionEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<AccountStateReconciledEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<ExternalAccountConnectionRestoredEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<ExternalExecutionReportEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<ExternalOrderCancelRejectEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<ExternalTradeEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<ExternalBalanceOperationEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<ExternalAccountFullSnapshotEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            .AddSingleton<IEventHandler<ExternalAccountOrdersSnapshotEvt>>(sp => sp.GetRequiredService<ExternalTradingEventsHandler>())
            
            .AddSingleton<HeartbeatsLogger>()
            .AddSingleton<IExternalEventHandler<AccountsServiceHeartbeatEvt>>(sp => sp.GetRequiredService<HeartbeatsLogger>())
            
            .AddSingleton<AsyncRequestsUniversalHandler>()
            .AddSingleton<IAsyncQueryHandler>(sp => sp.GetRequiredService<AsyncRequestsUniversalHandler>())
            ;
            
        if (addHostedService) services.AddHostedService(sp => sp.GetService<ExecutionService>()!);
        return services;
    }
}