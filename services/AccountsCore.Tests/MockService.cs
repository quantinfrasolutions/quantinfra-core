using AccountsCore;
using Common.Accounts.Abstractions;
using Common.Infrastructure.Abstractions;
using Disruptor.Dsl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Messaging.Json;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.WAL;
using QuantInfra.Common.StaticData.InMemory;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Services.AccountsCore.State;
using QuantInfra.Tests.Mocks;

namespace QuantInfra.Services.AccountsCore.Tests;

public static class MockService
{
    public static IServiceProvider BuildService() => new ServiceCollection()
        .AddLogging(conf =>
        {
            conf.AddConsole();
        })
        .AddSingleton<Config>(sp => new() { AccountServiceName = "AS" })
        // .AddSingleton<FinalizerConfig>(sp => new())
        .AddSingleton<DisruptorConfig>(sp => new())
        .AddJsonMessages("rabbitmq")
        .AddDefaultJsonSerializerSettings()
        .UseSingletonInMemoryBus()
        .AddSingleton<IAccountRecordsRepositoryReadonly, MockAccountRecordsRepositoryReadonly>()
        .AddSingleton<IStrategyRecordsRepositoryReadonly, MockStrategyRecordsRepositoryReadonly>()
        .AddAccountsCore()
        .AddSingleton<WalManager<AccountServiceState>>(sp => null!)
        .AddSingleton<AccountServiceState>(sc => new())
        .AddSingleton<MockManagementNotificationsClient>()
        .AddSingleton<IManagementNotificationsClient>(sp => sp.GetRequiredService<MockManagementNotificationsClient>())
        .AddSingleton<ReceiverFilter>(sp => new(
            sp.GetRequiredService<Disruptor<IncomingDisruptorMessage>>(),
            null, 
            sp.GetRequiredService<ILogger<ReceiverFilter>>(), 
            sp.GetRequiredService<IReceiverStateProvider>(),
            sp.GetRequiredService<IClock>())
        )
        
        .UseInMemoryStaticDataRepository()
        .BuildServiceProvider();
}