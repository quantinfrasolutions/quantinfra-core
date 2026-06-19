using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AccountsCore;
using Common.Utils.Reflection;
using Disruptor.Dsl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.Handlers;
using QuantInfra.Domain.AccountRecordsStateManager;
using QuantInfra.Domain.Accounts.Base;
using QuantInfra.Domain.Accounts.Base.CommandHandlers;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Accounts.Execution;
using QuantInfra.Domain.Commands.Strategies.AccountsService;
using QuantInfra.Domain.Events.Accounts.External;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.MarketData;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Accounts.ExecutionService;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Domain.StaticData;
using QuantInfra.Domain.StrategyRecordsStateManager;
using QuantInfra.Domain.VirtualExecution;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Accounts.ExternalAccounts;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Sdk.Trading.Positions;
using QuantInfra.Services.AccountsCore.CommandHandlers;
using QuantInfra.Services.AccountsCore.EventHandlers;
using QuantInfra.Services.AccountsCore.QueryHandlers;
using QuantInfra.Services.AccountsCore.State;
using DownstreamMessage = QuantInfra.Common.Messaging.Patterns.TopicMulticast.DownstreamMessage;
using GetAccount = QuantInfra.Domain.Queries.Accounts.AccountsService.GetAccount;
using GetAccountState = QuantInfra.Domain.Queries.Accounts.AccountsService.GetAccountState;
using QueryHandlers_GetAccountState = QuantInfra.Services.AccountsCore.QueryHandlers.GetAccountState;
using StateManagerConfig = QuantInfra.Domain.AccountRecords.AccountRecordsClientStateManager.StateManagerConfig;

namespace QuantInfra.Services.AccountsCore;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureAccountsCore(
        this IServiceCollection sc, 
        IConfiguration configuration, 
        string sectionName = "accounts-service",
        Action<Config>? configureAction = null
    ) => sc
        .Configure<Config>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<Config>(sc =>
        {
            var config = sc.GetRequiredService<IOptions<Config>>().Value;
            configureAction?.Invoke(config);
            return config;
        })
        .ConfigureWalManager(configuration);

    /// <summary>
    /// Requires: 
    /// </summary>
    public static IServiceCollection AddAccountsCore(this IServiceCollection sc, IClock? clock = null) => sc
        
        .AddSingleton<ReplayingClock>(new ReplayingClock(clock ?? SystemClock.Instance))
        .AddSingleton<IClock>(sp => sp.GetRequiredService<ReplayingClock>())
            
        .AddWalManager<AccountServiceState>()
        
        .AddSingleton<ParserConfig>(sp => new ParserConfig() { WritePerformanceMetrics = sp.GetRequiredService<Config>().WritePerformanceMetrics })
        .AddSingleton<Parser>(sp => new(sp.GetRequiredKeyedService<IMessageFactory>("rabbitmq"), sp.GetRequiredService<ParserConfig>()))
        
        // .AddSingleton<DownstreamFilter>()
        
        .AddSingleton<MulticastSender>(sp => new(
            sp.GetRequiredService<Config>(),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<ITransport<DownstreamMessage>>(),
            // sp.GetRequiredService<Disruptor<IncomingDisruptorMessage>>(),
            sp.GetRequiredService<IOutputToInputDisruptorPublisher>(),
            sp.GetRequiredService<IMulticastMessageFactory>(),
            sp.GetRequiredService<ILogger<MulticastSender>>()
        ))
        .AddSingleton<RequestSnapshotHandler>()
        .AddSingleton<IRequestSnapshotMessageHandler>(sp => sp.GetRequiredService<RequestSnapshotHandler>())
        
        .AddDisruptorPublishingSubscriber()
        
        .AddInputDisruptor()
        .AddOutputDisruptor()
        
        .AddSingleton<List<Assembly>>(sp => sp.GetRequiredService<ITypeResolver>().LoadedAssemblies.ToList()) // TODO
            
        .AddSingleton<StateManagerConfig>(sp =>
        {
            var conf = sp.GetRequiredService<Config>();
            return new() { AccountServiceName = conf.AccountServiceName };
        })
        .AddSingleton<AccountsServiceStateManagerConfig>(sp =>
        {
            var conf = sp.GetRequiredService<Config>();
            return new() { AccountsServiceName = conf.AccountServiceName };
        })
        
        .UseSingletonInMemoryBus()
        
        // .AddFinalizer("rabbitmq")
        
        .AddDisruptorAsyncQueryBus()
        
        .AddSingleton<ManagementEvents>()
        .AddSingleton<IEventHandler<TradingClientConfigurationChangedEvt>>(sp => sp.GetRequiredService<ManagementEvents>())
        
        .AddAccountRecordsClientStateManager()
        .AddSingleton<IEventHandler<AccountCreatedEvt>>(sp => sp.GetRequiredService<ManagementEvents>())
        
        .AddStrategyRecordsClientStateManagerForAccountsService()
        .AddSingleton<IEventHandler<StrategyCreatedEvt>>(sp => sp.GetRequiredService<ManagementEvents>())
        
        .AddSingleton<AccountsFactory>()
        .AddSingleton<IQueryHandler<GetAccount, IAccount>>(sp => sp.GetRequiredService<AccountsFactory>())
        .AddSingleton<IQueryHandler<GetAccount, IBrokerAccount>>(sp => sp.GetRequiredService<AccountsFactory>())
        .AddSingleton<IQueryHandler<GetAccountBase, AccountBase>>(sp => sp.GetRequiredService<AccountsFactory>())
        .AddSingleton<IConfigurableLoggingModule>(sp => sp.GetRequiredService<AccountsFactory>())
        
        .AddSingleton<StrategiesFactory>()
        .AddSingleton<IQueryHandler<GetStrategy, IStrategy?>>(sp => sp.GetRequiredService<StrategiesFactory>())
        .AddSingleton<IQueryHandler<GetStrategyConcreteImplementation, QuantInfra.Domain.Strategies.Strategy?>>(sp => sp.GetRequiredService<StrategiesFactory>())
        
        .AddCachingStaticDataRepository()
        
        .AddSingleton<Strategies>()
        .AddSingleton<ICommandHandler<UpdateLastCalculationTsCmd>>(sp => sp.GetRequiredService<Strategies>())
        .AddSingleton<ICommandHandler<UpdateStrategyInternalStateCmd>>(sp => sp.GetRequiredService<Strategies>())
        
        .AddLastContractPricesStorage()
        .AddSingleton<ICommandHandler<QuantInfra.Domain.Commands.Accounts.AccountsService.ProcessBalanceOperationCmd>, ProcessBalanceOperationCmdHandler>()
        
        .AddSingleton<TradingCommandsHandler>()
        .AddSingleton<ICommandHandler<QuantInfra.Domain.Commands.Accounts.AccountsService.NewOrderCmd>>(sp => sp.GetRequiredService<TradingCommandsHandler>())
        .AddSingleton<ICommandHandler<QuantInfra.Domain.Commands.Accounts.AccountsService.ReplaceOrderCmd>>(sp => sp.GetRequiredService<TradingCommandsHandler>())
        .AddSingleton<ICommandHandler<QuantInfra.Domain.Commands.Accounts.AccountsService.CancelOrderCmd>>(sp => sp.GetRequiredService<TradingCommandsHandler>())
        .AddSingleton<IConfigurableLoggingModule>(sp => sp.GetRequiredService<TradingCommandsHandler>())
        
        .AddSingleton<EndOfDay>()
        .AddSingleton<ICommandHandler<QuantInfra.Domain.Commands.Accounts.AccountsService.RunEndOfDayCmd>>(sp => sp.GetRequiredService<EndOfDay>())
        
        .AddSingleton<ExternalAccountsEventsHandler>()
        // .AddSingleton<IAsyncQueryResponseHandler<GetExternalAccountSnapshot, ExternalAccountFullSnapshot>>(sp => sp.GetRequiredService<ExternalAccountsEventsHandler>())
        .AddSingleton<IExternalEventHandler<ExternalExecutionReportEvt>>(sp => sp.GetRequiredService<ExternalAccountsEventsHandler>())
        .AddSingleton<IExternalEventHandler<ExternalTradeEvt>>(sp => sp.GetRequiredService<ExternalAccountsEventsHandler>())
        .AddSingleton<IExternalEventHandler<ExternalBalanceOperationEvt>>(sp => sp.GetRequiredService<ExternalAccountsEventsHandler>())
        .AddSingleton<IExternalEventHandler<ExternalAccountConnectionRestoredEvt>>(sp => sp.GetRequiredService<ExternalAccountsEventsHandler>())
        .AddSingleton<IExternalEventHandler<ExecutionServiceMissedVersionEvt>>(sp => sp.GetRequiredService<ExternalAccountsEventsHandler>())
        .AddSingleton<IExternalEventHandler<ExternalOrderCancelRejectEvt>>(sp => sp.GetRequiredService<ExternalAccountsEventsHandler>())
        .AddSingleton<IExternalEventHandler<ExternalAccountFullSnapshotEvt>>(sp => sp.GetRequiredService<ExternalAccountsEventsHandler>())
        .AddSingleton<IExternalEventHandler<ExternalAccountOrdersSnapshotEvt>>(sp => sp.GetRequiredService<ExternalAccountsEventsHandler>())
        
        .UseVirtualExecutorWithSingletonHandlers()
        
        .AddSingleton<QueryHandler>()
        .AddSingleton<IQueryHandler<QueryHandlers_GetAccountState, AccountBaseState?>>(sp => sp.GetRequiredService<QueryHandler>())
        .AddSingleton<IQueryHandler<GetAccountState, AccountStateReadonly?>>(sp => sp.GetRequiredService<QueryHandler>())
        .AddSingleton<IQueryHandler<GetBrokerAccountState, BrokerAccountStateReadonly?>>(sp => sp.GetRequiredService<QueryHandler>())
        .AddSingleton<IQueryHandler<GetAccountIdsForEndOfDay, IReadOnlyCollection<int>>>(sp => sp.GetRequiredService<QueryHandler>())
        .AddSingleton<IQueryHandler<GetActiveVirtualExecutorOrders, IReadOnlyCollection<OrderStatus>>>(sp => sp.GetRequiredService<QueryHandler>())
        .AddSingleton<IQueryHandler<GetBalances, IReadOnlyDictionary<int, decimal>>>(sp => sp.GetRequiredService<QueryHandler>())
        // .AddSingleton<IQueryHandler<Domain.Queries.Accounts.GetAccountState, BrokerAccountState>>(sp => sp.GetRequiredService<GetAccountState>())
        // .AddSingleton<IQueryHandler<Domain.Queries.Accounts.GetAccountState, StrategySubaccountState>>(sp => sp.GetRequiredService<GetAccountState>())
        // .AddSingleton<IQueryHandler<Domain.Queries.Accounts.GetAccountState, ExecutableSubaccountState>>(sp => sp.GetRequiredService<GetAccountState>())
        .AddSingleton<IQueryHandler<GetActiveOrders, IReadOnlyCollection<OrderStatus>>>(sp => sp.GetRequiredService<QueryHandler>()) 
        .AddSingleton<IQueryHandler<GetActiveVirtualExecutorOrders, IReadOnlyCollection<OrderStatus>>>(sp => sp.GetRequiredService<QueryHandler>())
        .AddSingleton<IQueryHandler<GetStrategyState, IStrategyStateReadonly?>>(sp => sp.GetRequiredService<QueryHandler>())
        .AddSingleton<IQueryHandler<global::QuantInfra.Domain.Queries.Strategies.AccountsService.GetStrategyState, StrategyStateReadonly?>>(sp => sp.GetRequiredService<QueryHandler>())
        .AddSingleton<IQueryHandler<GetPositions, IReadOnlyCollection<Position>>>(sp => sp.GetRequiredService<QueryHandler>())
        
        .AddSingleton<MarketDataEventsHandler>()
        .AddSingleton<IEventHandler<ContractLastPriceUpdatedEvt>>(sp => sp.GetRequiredService<MarketDataEventsHandler>())
        
        .AddExecutionAccounts()
        
        .AddEventsForwarder()
        
        .AddSingleton<AccountServiceState>(new AccountServiceState())
        .AddSingleton<IEventIdProvider>(sp => sp.GetRequiredService<AccountServiceState>())
        .AddSingleton<IBalanceOperationIdProvider>(sp => sp.GetRequiredService<AccountServiceState>())
        .AddSingleton<IOrderIdProvider>(sp => sp.GetRequiredService<AccountServiceState>())
        .AddSingleton<IExecIdProvider>(sp => sp.GetRequiredService<AccountServiceState>())
        .AddSingleton<ITradeIdProvider>(sp => sp.GetRequiredService<AccountServiceState>())
        .AddSingleton<IReceiverStateProvider>(sp => sp.GetRequiredService<AccountServiceState>())
        .AddSingleton<ILastContractPricesStore>(sp => sp.GetRequiredService<AccountServiceState>())
        .AddSingleton<IStaticDataRepositoryStateStore>(sp => sp.GetRequiredService<AccountServiceState>())
        .AddSingleton<IAccountRecordsStore>(sp => sp.GetRequiredService<AccountServiceState>())
        .AddSingleton<IStrategyRecordsStore>(sp => sp.GetRequiredService<AccountServiceState>())
        // .AddSingleton<IAccountStatesRepositoryV6>(sp => sp.GetRequiredService<AccountServiceState>())
        
        .AddSingleton<HeartbeatEvents>()
        // .AddSingleton<IEventHandler>(sp => sp.GetRequiredService<HeartbeatEvents>())
        .AddSingleton<ICommandHandler<ProcessHeartbeatCmd>>(sp => sp.GetRequiredService<HeartbeatEvents>())
        
        .AddSingleton<Bpl>()
    
        .AddSingleton<Persister>()
        
        .AddSingleton<AccountsService>()
        .AddHostedService(sp => sp.GetRequiredService<AccountsService>());

    // public static IServiceCollection UseRabbitMqNotifications(this IServiceCollection sc) => sc
    //     .AddSingleton<ManagementNotificationsClient>()
    //     .AddSingleton<IManagementNotificationsClient>(sp => sp.GetRequiredService<ManagementNotificationsClient>());
}