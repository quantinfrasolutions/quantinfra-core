using System;
using System.Collections.Generic;
using System.Linq;
using Common.Infrastructure.Abstractions;
using Common.Utils.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.Handlers;
using QuantInfra.Common.Strategies;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Domain.Accounts.AccountStateClientManager;
using QuantInfra.Domain.Events.Accounts.AccountsService;
using QuantInfra.Domain.HostedStrategies;
using QuantInfra.Domain.StaticData;
using QuantInfra.Domain.Strategies.StrategyStateClientManager;
using QuantInfra.Domain.StrategyRecordsStateManager;
using StrategiesCore;
using Config = QuantInfra.Services.StrategiesCore.Config;

namespace QuantInfra.Services.StrategiesCore;

public static class ConfigurationExtensions
{
    public const string StrategiesTypeResolverKey = "StrategiesTypeResolver";
    
    public static IServiceCollection ConfigureStrategiesCore(this IServiceCollection sc, IConfiguration configuration, 
        string sectionName = "strategies-service", 
        Action<Config>? configureAction = null
    ) => sc
        .Configure<Config>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<Config>(sc =>
        {
            var config = sc.GetRequiredService<IOptions<Config>>().Value;
            configureAction?.Invoke(config);
            return config;
        });
    
    public static IServiceCollection AddStrategiesCore(this IServiceCollection sc, bool addHostedService = true, 
        IEnumerable<string>? strategyAssemblyNames = null)
    {
        sc
            .AddSingleton<StrategiesServiceStateManagerConfig>(sp =>
            {
                var conf = sp.GetRequiredService<Config>();
                return new() { StrategiesServiceName = conf.StrategiesServiceName };
            })
            
            .AddSingleton<ParserConfig>(sp => new ParserConfig() { WritePerformanceMetrics = sp.GetRequiredService<Config>().WritePerformanceMetrics })
            .AddSingleton<Parser>(sp => new(sp.GetRequiredKeyedService<IMessageFactory>("rabbitmq"), sp.GetRequiredService<ParserConfig>()))
            
            .UseSingletonInMemoryBus()
            .AddDisruptorAsyncQueryBus()
            
            .AddStrategyRecordsClientStateManagerForStrategiesService()
            .AddAccountsTradingApi()
            .AddStrategiesStateClientManager()
            
            .AddSingleton<StrategiesServiceState>(new StrategiesServiceState())
            
            .AddSingleton<HostedStrategiesFactory>(sp => new(
                sp.GetRequiredKeyedService<ITypeResolver>(StrategiesTypeResolverKey),
                sp.GetRequiredService<ILoggerFactory>()
            ))
            .AddSingleton<IHostedStrategiesFactory>(sp => sp.GetRequiredService<HostedStrategiesFactory>()) 
            
            .AddCachingStaticDataRepository()
            .AddInMemoryStaticDataStore()
            
            .AddInputDisruptor()
            .AddOutputDisruptor()
            
            .AddSingleton<AsyncRequestsUniversalHandler>()
            .AddSingleton<IAsyncQueryHandler>(sp => sp.GetRequiredService<AsyncRequestsUniversalHandler>())
            
            .AddSingleton<HeartbeatsLogger>()
            .AddSingleton<IExternalEventHandler<AccountsServiceHeartbeatEvt>>(sp => sp.GetRequiredService<HeartbeatsLogger>())
            
            // .AddKeyedSingleton<ITypeResolver>(StrategiesTypeResolverKey,
            //     (sp, _) => new MultipleAssembliesTypeResolver(strategyAssemblyNames ?? Enumerable.Empty<string>())
            // )
            
            // .AddSingleton<ITypeResolver>(sp => new MultipleAssembliesTypeResolver(
            //     new List<string>
            //         {
            //             "Common.Messaging",
            //             "Common.EventSourcing",
            //             "Common.ServiceBase",
            //             "Domain.Events.Accounts.Management",
            //             "Domain.Events.Accounts.AccountsService",
            //             "Domain.Events.Strategies.Management",
            //             "Domain.Events.Strategies.AccountsService",
            //             "Domain.Events.MarketData",
            //             "Domain.Queries.Accounts.AccountsService",
            //             "Domain.Commands.Accounts.AccountsService",
            //             "QuantInfra.Domain.Commands.StaticData",
            //         }
            // ))
            
            .AddSingleton<Bpl>()
            .AddHostedStrategiesRunner()
            .AddSingleton<StrategiesService>();

        if (addHostedService) sc.AddHostedService(sp => sp.GetRequiredService<StrategiesService>());

        return sc;
    }
    
    // public static IServiceCollection UseRabbitMqNotifications(this IServiceCollection sc) => sc
    //     .AddSingleton<ManagementNotificationsClient>()
    //     .AddSingleton<IManagementNotificationsClient>(sp => sp.GetRequiredService<ManagementNotificationsClient>());
}