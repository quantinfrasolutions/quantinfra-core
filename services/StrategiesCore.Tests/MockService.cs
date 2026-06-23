using Common.Utils.Reflection;
using Disruptor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Json;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.StaticData.InMemory;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Tests.Mocks;

namespace QuantInfra.Services.StrategiesCore.Tests;

public static class MockService
{
    public static IServiceProvider BuildService(bool addHostedService) => new ServiceCollection()
        .AddLogging(conf =>
        {
            conf.AddConsole();
        })
        
        .AddSingleton<IComponentExceptionHandler, FailFastExceptionHandler>()
        
        .AddSingleton<Config>(sp => new() { StrategiesServiceName = "SS1" })
        
        .AddSingleton<HostedStrategiesRunnerConfig>(sp => new()
        {
            
        })
        
        .AddSingleton<DisruptorConfig>(sp => new())
        
        .AddStrategiesCore(addHostedService)
        .AddJsonMessages("rabbitmq")
        .AddJsonMessages()
        .AddDefaultJsonSerializerSettings()
        .AddJsonDealerRouterMessageFactory(sp => sp.GetRequiredService<Config>().StrategiesServiceName)
        
        .AddSingleton<IAccountRecordsRepositoryReadonly, MockAccountRecordsRepositoryReadonly>()
        .AddSingleton<IStrategyRecordsRepositoryReadonly, MockStrategyRecordsRepositoryReadonly>()
        .AddSingleton<MockManagementNotificationsClient>()
        .AddSingleton<IManagementNotificationsClient>(sp => sp.GetRequiredService<MockManagementNotificationsClient>())
        .UseInMemoryStaticDataRepository()
        .AddSingleton<IClock>(sp => SystemClock.Instance)
        
        .UseSingletonInMemoryBus()
        
        .AddSingleton<MockAccountsServiceApi>()
        .AddSingleton<IAccountsServiceApi>(sp => sp.GetRequiredService<MockAccountsServiceApi>())
        // .AddSingleton<MockAccountsApi>()
        // .AddSingleton<IAccountsApi>(sp => sp.GetRequiredService<MockAccountsApi>())
        
        .AddSingleton<MockMarketDataHistoryProvider>()
        .AddSingleton<IMarketDataHistoryProvider>(sp => sp.GetRequiredService<MockMarketDataHistoryProvider>())
        
        .AddSingleton<MockMarketDataClient>()
        .AddSingleton<IMarketDataClient>(sp => sp.GetRequiredService<MockMarketDataClient>())
        
        .AddSingleton<ITypeResolver>(sp => new MultipleAssembliesTypeResolver(
            new List<string>
                {
                    "QuantInfra.Common.Messaging",
                    "QuantInfra.Common.EventSourcing",
                    "QuantInfra.Common.ServiceBase",
                    "QuantInfra.Domain.Events.Accounts.Management",
                    "QuantInfra.Domain.Events.Accounts.AccountsService",
                    "QuantInfra.Domain.Events.Strategies.Management",
                    "QuantInfra.Domain.Events.Strategies.AccountsService",
                    "QuantInfra.Domain.Events.MarketData",
                    "QuantInfra.Domain.Queries.Accounts.AccountsService",
                    "QuantInfra.Domain.Commands.Accounts.AccountsService",
                    "QuantInfra.Domain.Commands.StaticData",
                }
        ))
        .AddKeyedSingleton<ITypeResolver>(
            ConfigurationExtensions.StrategiesTypeResolverKey,
            (sp, _) => new MultipleAssembliesTypeResolver(new List<string>()
            {
                "Tests.Mocks",
            })
        )
        
        // .AddSingleton<DealerConfig>(new DealerConfig { SenderCompId = "SS1", Uri = "tcp://localhost:7777" })
        // .AddZeroMqDisruptorAccountsServiceApi()
        
        // .AddSingleton<SubscriberConfig>(new SubscriberConfig { Endpoints = new List<string>() { "tcp://localhost:7778" } })
        .AddSingleton<Sender>()
        .AddSingleton<ITransport<DownstreamMessage>>(sp => new MockTransport<DownstreamMessage>())
        
        
        .BuildServiceProvider();
}