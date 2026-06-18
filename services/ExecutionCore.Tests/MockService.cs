using Common.EventSourcing;
using Common.Infrastructure.Abstractions;
using Common.Messaging;
using Common.Messaging.Json;
using Common.Trading.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuanInfra.Common.ServiceBase;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.StaticData.Abstractions;
using QuantInfra.Tests.Mocks;

namespace ExecutionCore.Tests;

public static class MockService
{
    public static IServiceProvider BuildService(bool addHostedService) => new ServiceCollection()
        .AddLogging(conf =>
        {
            conf.AddConsole();
        })
        
        .AddSingleton<Config>(sp => new() { ExecutionServiceName = "ES1" })
        
        .AddSingleton<DisruptorConfig>(sp => new())
        
        .AddExecutionService(addHostedService, ["Tests.Mocks"])
        .AddJsonMessages("rabbitmq")
        .AddJsonMessages()
        .AddDefaultJsonSerializerSettings()
        
        .UseInMemoryStaticDataRepository()
        .AddSingleton<IClock>(sp => SystemClock.Instance)
        
        .UseSingletonInMemoryBus()
        
        .AddSingleton<MockAccountsServiceApi>()
        .AddSingleton<IAccountsServiceApi>(sp => sp.GetRequiredService<MockAccountsServiceApi>())
        // .AddSingleton<Sender>()
        .AddSingleton<ITransport<DownstreamMessage>>(sp => new MockTransport<DownstreamMessage>())
        
        .AddSingleton<MockTradingAccountsRepositoryReadonly>()
        .AddSingleton<ITradingAccountsRepositoryReadonly>(sp => sp.GetRequiredService<MockTradingAccountsRepositoryReadonly>())
        
        .AddSingleton<ITransport<DownstreamMessage>>(sp => new MockTransport<DownstreamMessage>())
        
        
        .BuildServiceProvider();
}