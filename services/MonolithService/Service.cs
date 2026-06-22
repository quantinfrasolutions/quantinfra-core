using System.Reflection;
using Common.Infrastructure.Abstractions;
using Common.Utils.Reflection;
using DAL.MarketDataHistory.Configuration;
using Databases.MarketDataHistory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NodaTime;
using Npgsql;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.InProcess;
using QuantInfra.Common.Messaging.InProcess.Messages.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.InProcess.Messages.TopicMulticast;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Databases.Main;
using QuantInfra.Services.AccountsCore;
using Quartz;
using QuantInfra.Common.Messaging.Json;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Domain.Accounts.AccountStateClientManager;
using QuantInfra.Domain.HostedStrategies;
using QuantInfra.Services.StrategiesCore;
using TransportFactory = QuantInfra.Common.Messaging.InProcess.TransportFactory;
using Disruptor.Dsl;
using Microsoft.EntityFrameworkCore;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Connectors.Binance.Futures.Usdm;
using QuantInfra.Databases.MarketDataHistory;
using QuantInfra.Services.ExecutionCore;
using QuantInfra.Services.MarketData;
using QuantInfra.Services.MonolithService.AccountsService;
using IncomingDisruptorMessage = QuantInfra.Common.ServiceBase.IncomingDisruptorMessage;
using MarketDataClient = QuantInfra.Services.MonolithService.MDS.MarketDataClient;

namespace QuantInfra.Services.MonolithService;

public class Service : IHostedService
{
    private const string AsName = "AS"; 
    private const string MdsName = "mds";
    private const string UsdmFuturesName = "binance-usdm-mds";
    private const string UsdmFuturesOrderBooksName = "binance-usdm-mds-ob";
    
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly Config _config;
    private readonly IInfrastructureRepositoryReadonly _infraRepositoryReadonly;
    private readonly IInfrastructureRepository _infraRepository;
    private readonly ILogger<Service> _logger;
    private readonly Dictionary<string, HostedComponent> _components = new();

    public Service(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        Config config,
        IInfrastructureRepositoryReadonly infraRepositoryReadonly,
        IInfrastructureRepository infraRepository,
        ILogger<Service> logger
    )
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _config = config;
        _infraRepositoryReadonly = infraRepositoryReadonly;
        _infraRepository = infraRepository;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureDatabasesMigrated();
        
        var (location, accSvcInstance, strSvc, 
            execSvc, mdClients) = await EnsureInfrastructureSetup();

        var accSvcComponent = new HostedComponent(accSvcInstance.Name, _logger,
            () => SetupAccountsService(accSvcInstance),
            sp => sp.GetRequiredService<IEnumerable<IHostedService>>()
        );
        await accSvcComponent.StartAsync();
        
        List<Assembly>? strategyAssemblies = null;
        try
        {
            PluginLoadContext.PreloadQuantInfraAssemblies();

            strategyAssemblies = _config.StrategyDllPaths
                .Select(path =>
                {
                    var loadContext = new PluginLoadContext(path);
                    var assembly = loadContext.LoadFromAssemblyName(new(Path.GetFileNameWithoutExtension(path)));
                    assembly.AssertCompatible();
                    return assembly;
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading strategies");
        }
        _logger.LogInformation("Loaded strategy dlls");
        if (strategyAssemblies is not null)
        {
            var resolver = new MultipleAssembliesTypeResolver(strategyAssemblies);
            var ssComponents = strSvc.Select(s => new HostedComponent(s.Name, _logger,
                () => SetupStrategiesService(s, accSvcComponent.Name, resolver),
                sp => [sp.GetRequiredService<StrategiesCore.StrategiesService>()]
            )).ToList();

            foreach (var c in ssComponents) _components.Add(c.Name, c);
        }

        var esComponents = execSvc.Select(s => new HostedComponent(s.Name, _logger,
            () => SetupExecutionService(s),
            sp => sp.GetRequiredService<IEnumerable<IHostedService>>())
        ).ToList();
        foreach (var es in esComponents) _components.Add(es.Name, es);

        if (_config.EnableMarketDataService)
        {
            var mds = new HostedComponent(MdsName, _logger,
                () => SetupMarketDataService(),
                sp => sp.GetRequiredService<IEnumerable<IHostedService>>()
            );
            _components.Add(mds.Name, mds);
        }

        if (_config.EnableBinanceUsdmMarketDataService)
        {
            var usdmMd = new HostedComponent(UsdmFuturesName, _logger,
                () => SetupBinanceFuturesUsdmMarketDataClient(UsdmFuturesName),
                sp => sp.GetRequiredService<IEnumerable<IHostedService>>()
            );
            _components.Add(usdmMd.Name, usdmMd);
        }
        
        if (_config.EnableBinanceUsdmPublicMarketDataService)
        {
            var usdmMd = new HostedComponent(UsdmFuturesOrderBooksName, _logger,
                () => SetupBinanceFuturesUsdmMarketDataClient(UsdmFuturesOrderBooksName),
                sp => sp.GetRequiredService<IEnumerable<IHostedService>>()
            );
            _components.Add(usdmMd.Name, usdmMd);
        }

        await Task.WhenAll(_components.Values.Select(c => c.StartAsync()));
        _components.Add(accSvcComponent.Name, accSvcComponent);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping application");
        await Task.WhenAll(_components.Where(kv => kv.Key != AsName && kv.Value.Status == ComponentStatus.Running).ToList()
            .Select(kv  => kv.Value.StopAsync())
        );
        if (_components.TryGetValue(AsName, out var accSvc) && accSvc.Status == ComponentStatus.Running)
        {
            await accSvc.StopAsync();
        }
        _logger.LogInformation("Application stopped");
    }

    private Task EnsureDatabasesMigrated() => Task.WhenAll(
        Task.Run(async () =>
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MainContext>();
            await context.Database.EnsureCreatedAsync();
            await context.Database.MigrateAsync();
            _logger.LogInformation("Main database migrated");
        }),
        Task.Run(async () =>
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MDTimescaleContextDesign>();
            await context.Database.EnsureCreatedAsync();
            await context.Database.MigrateAsync();
            _logger.LogInformation("Market data database migrated");
        })
    );
    

    private async Task<(Location location, AccountServiceInstance accSvc, IReadOnlyCollection<StrategiesServiceInstance> strSvc, IReadOnlyCollection<ExecutionServiceInstance> execSvc, IReadOnlyCollection<MarketDataClientInstance> mdClients)> EnsureInfrastructureSetup()
    {
        var location = (await _infraRepositoryReadonly.GetLocationsAsync()).SingleOrDefault();
        if (location is null)
        {
            location = new() { Name = "local" };
            await _infraRepository.CreateLocationAsync(location);
        }
        
        var accSvc = (await _infraRepositoryReadonly.GetAccountServiceInstancesAsync()).SingleOrDefault();
        if (accSvc == null)
        {
            accSvc = new() { LocationName = "local", Name = AsName };
            await _infraRepository.CreateAccountServiceInstanceAsync(accSvc);
        }
        
        var strSvc = await _infraRepositoryReadonly.GetStrategiesServiceInstancesAsync();
        if (strSvc.Count == 0)
        {
            var svc = new StrategiesServiceInstance { LocationName = "local", Name = "SS" };
            await _infraRepository.CreateStrategiesServiceInstanceAsync(svc);
            strSvc = new List<StrategiesServiceInstance> { svc };
        }
        strSvc = strSvc
            .Where(s => 
                (_config.EnabledStrategiesServices.Count == 0 || _config.EnabledStrategiesServices.Contains(s.Name))
                && !_config.DisabledStrategiesServices.Contains(s.Name)
            ).ToList();
        _logger.LogInformation("Serving {cnt} Strategy Services", strSvc.Count);
        
        var execSvc = await _infraRepositoryReadonly.GetExecutionServiceInstancesAsync();
        if (execSvc.Count == 0)
        {
            var svc = new ExecutionServiceInstance { LocationName = "local", Name = "ES" };
            await _infraRepository.CreateExecutionServiceInstanceAsync(svc);
            execSvc = new List<ExecutionServiceInstance> { svc };
        }
        execSvc = execSvc
            .Where(e => 
                (_config.EnabledExecutionServices.Count == 0 || _config.EnabledExecutionServices.Contains(e.Name))
                && !_config.DisabledExecutionServices.Contains(e.Name)
            )
            .ToList();
        _logger.LogInformation("Serving {cnt} Execution Services", execSvc.Count);
        
        var mdClients = (await _infraRepositoryReadonly.GetMarketDataClientInstancesAsync()).ToList();
        if (_config.EnableMarketDataService && mdClients.All(c => c.Name != MdsName))
        {
            var mds = new MarketDataClientInstance { LocationName = "local", Name = MdsName };
            await _infraRepository.CreateMarketDataClientInstanceAsync(mds);
            mdClients.Add(mds);
        }

        if (_config.EnableBinanceUsdmMarketDataService && mdClients.All(c => c.Name != UsdmFuturesName))
        {
            var usdm = new MarketDataClientInstance { LocationName = "local", Name = UsdmFuturesName };
            await _infraRepository.CreateMarketDataClientInstanceAsync(usdm);
            mdClients.Add(usdm);
        }
        
        if (_config.EnableBinanceUsdmMarketDataService && mdClients.All(c => c.Name != UsdmFuturesOrderBooksName))
        {
            var usdmOb = new MarketDataClientInstance { LocationName = "local", Name = UsdmFuturesOrderBooksName };
            await _infraRepository.CreateMarketDataClientInstanceAsync(usdmOb);
            mdClients.Add(usdmOb);
        }

        return (location, accSvc, strSvc, execSvc, mdClients);
    }
    
    private IServiceProvider SetupAccountsService(AccountServiceInstance instance)
    {
        var transportFactory = _serviceProvider.GetRequiredService<TransportFactory>();
        var topology = _serviceProvider.GetRequiredService<Topology>();
        // var asMulticastSender = transportFactory.CreateMulticastTransport(instance.Name, TODO);
        
        
        return new ServiceCollection()
            .AddLogging(c =>
            {
                c.ClearProviders();
                c.AddNLog();
                LogManager.Configuration = new NLogLoggingConfiguration(_configuration.GetSection("nlog"));
            })
            
            .ConfigureMainDb(_serviceProvider.GetRequiredService<NpgsqlDataSource>())
            .AddMainDbContext()
            .UseSingletonMainDbAccountRecordsRepositoryReadonly()
            .UseSingletonMainDbStrategyRecordsRepositoryReadonly()
            .UseMainDbStaticDataProvider()
            .UseMainDbEventsRepository()
            .UseMainDbPersistentEventStorage()

            .ReuseInProcessMessaging(_serviceProvider)
            .AddSingleton<MarketDataClient>()
            .AddSingleton<IMarketDataClient>(sp => sp.GetRequiredService<MarketDataClient>())

            .AddSingleton<ITypeResolver>(new MultipleAssembliesTypeResolver(Array.Empty<string>()))

            .ConfigureWalManager(_configuration)

            .ConfigureDisruptors(_configuration.GetSection("accounts-service"))

            .AddSingleton<AccountsService.ManagementNotificationsClient>()
            .AddSingleton<IManagementNotificationsClient>(sp => sp.GetRequiredService<AccountsService.ManagementNotificationsClient>())
            .AddSingleton<IIncomingTransport>(sp => sp.GetRequiredService<AccountsService.ManagementNotificationsClient>())
            
            .AddSingleton<ReceiverFilter>()
            
            .AddSingleton<IOutputToInputDisruptorPublisher, AccountsService.OutputToInputDisruptorPublisher>()
            
            .AddJsonMessages("rabbitmq")
            .AddDefaultJsonSerializerSettings("rabbitmq")
            .AddJsonMessages("serializer")
            .AddDefaultJsonSerializerSettings("serializer")
            .AddJsonMessages()
            .AddDefaultJsonSerializerSettings()
            .AddSingleton<IMulticastMessageFactory, MulticastMessageFactory>()
            
            .AddSingleton<ITransport<QuantInfra.Common.Messaging.Patterns.TopicMulticast.DownstreamMessage>>(sp =>
                transportFactory.CreateMulticastTransport(instance.Name, sp.GetRequiredService<Disruptor<IncomingDisruptorMessage>>()))
            .AddSingleton<Router>(sp => new(
                instance.Name, 
                topology, 
                sp.GetRequiredService<Disruptor<IncomingDisruptorMessage>>(), 
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IReceiverStateProvider>(),
                _serviceProvider.GetRequiredService<IClock>())
            )
            .AddSingleton<ITransport<QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.ControlMessage>>(sp => sp.GetRequiredService<Router>())
            .AddSingleton<IIncomingTransport>(sp => sp.GetRequiredService<Router>())
            .AddSingleton<ReceiverFilter>(sp => sp.GetRequiredService<Router>().ReceiverFilter)
            
            .AddQuartz()
            .AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = false;
            })
            
            .AddSingleton<QuantInfra.Common.ServiceBase.Handlers.Serializer, Serializer>()
            
            .AddSingleton<ITypeResolver>(sp => new MultipleAssembliesTypeResolver(new string[]
            {
                "QuantInfra.Common.Messaging",
                "QuantInfra.Common.Messaging.InProcess",
                "QuantInfra.Common.EventSourcing",
                "QuantInfra.Common.ServiceBase",
                "QuantInfra.Domain.Commands.Accounts.AccountsService",
                "QuantInfra.Domain.Commands.Strategies.AccountsService",
                "QuantInfra.Services.AccountsCore",
                "QuantInfra.Domain.Queries.Accounts.AccountsService",
                "QuantInfra.Domain.Queries.Strategies.AccountsService",
                "QuantInfra.Domain.Events.Accounts.Management",
                "QuantInfra.Domain.Events.Accounts.External",
                "QuantInfra.Domain.Events.Strategies.Management",
                "QuantInfra.Domain.Events.MarketData",
                "QuantInfra.Domain.Commands.StaticData",
            }))

            .ConfigureAccountsCore(
                _configuration.GetSection("accounts-service"),
                configureAction: conf =>
                {
                    conf.AccountServiceName = instance.Name;
                    conf.Monolith = true;
                    conf.SingleHost = true;
                })
            .AddAccountsCore()
            
            .AddSingleton<IHostApplicationLifetime>(_serviceProvider.GetRequiredService<IHostApplicationLifetime>())
            
            .BuildServiceProvider();

        // .AddQuartz()
        // .AddQuartzHostedService(options =>
        // {
        //     // when shutting down we want jobs to complete gracefully
        //     options.WaitForJobsToComplete = false;
        // })

        // HealthChecks
        // .Configure<HeartbeatsProcessedConfig>(conf =>
        //     configuration.GetSection("health-checks").GetSection("heartbeats-processed").Bind(conf)
        // )
        // .AddSingleton<HeartbeatsProcessedConfig>(sp =>
        //     sp.GetService<IOptions<HeartbeatsProcessedConfig>>()?.Value ?? new())
        //
        // .Configure<MarketDataProcessedConfig>(conf =>
        //     configuration.GetSection("health-checks").GetSection("marketdata-processed").Bind(conf)
        // )
        // .AddSingleton<MarketDataProcessedConfig>(sp =>
        //     sp.GetService<IOptions<MarketDataProcessedConfig>>()?.Value ?? new())
    }

    private IServiceProvider SetupStrategiesService(
        StrategiesServiceInstance instance, 
        string asName,
        MultipleAssembliesTypeResolver resolver
    )
    {
        var sc = new ServiceCollection()
            .AddLogging(c =>
            {
                c.ClearProviders();
                c.AddNLog();
                LogManager.Configuration = new NLogLoggingConfiguration(_configuration.GetSection("nlog"));
            })
            
            .ReuseInProcessMessaging(_serviceProvider)
            
            .ConfigureMainDb(_serviceProvider.GetRequiredService<NpgsqlDataSource>())
            .AddMainDbContext()
            .UseSingletonMainDbAccountRecordsRepositoryReadonly()
            .UseSingletonMainDbStrategyRecordsRepositoryReadonly()
            .UseMainDbStaticDataProvider()
            
            .AddJsonMessages("rabbitmq")
            .AddDefaultJsonSerializerSettings("rabbitmq")
            .AddDefaultJsonSerializerSettings()
            
            .AddSingleton<ITypeResolver>(sp => new MultipleAssembliesTypeResolver(Array.Empty<string>())) // HACK, it is required by wiring, but not used
            .AddKeyedSingleton<ITypeResolver>(StrategiesCore.ConfigurationExtensions.StrategiesTypeResolverKey, resolver)
            
            .AddSingleton<IClock>(SystemClock.Instance)

            .ConfigureMarketDataHistoryDb(_serviceProvider.GetRequiredService<MDDatasource>())
            .AddMarketDataHistoryDbContext()
            .UseMarketDataHistoryProviderDAL()

            .ConfigureDisruptors(_configuration.GetSection("strategies-service"))

            .ConfigureHostedStrategies(_configuration.GetSection("strategies-service"))
            
            .AddSingleton<StrategiesService.ManagementNotificationsClient>()
            .AddSingleton<IManagementNotificationsClient>(sp => sp.GetRequiredService<StrategiesService.ManagementNotificationsClient>())
            .AddSingleton<IIncomingTransport>(sp => sp.GetRequiredService<StrategiesService.ManagementNotificationsClient>())
            
            .AddSingleton<AccountsServiceApiConfig>(new AccountsServiceApiConfig { AccountsServiceName = "AS", ClientName = instance.Name, })
            .AddSingleton<AccountsServiceApi>()
            .AddSingleton<IAccountsServiceApi>(sp => sp.GetRequiredService<AccountsServiceApi>())
            .AddSingleton<Sender>(sp => sp.GetRequiredService<AccountsServiceApi>().Sender)
            .AddDealerMessageFactory(instance.Name)
            
            .AddSingleton<MarketDataClient>()
            .AddSingleton<IMarketDataClient>(sp => sp.GetRequiredService<MarketDataClient>())

            .ConfigureAccountStateClientManager(_configuration.GetSection("strategies-service"))

            .ConfigureStrategiesCore(
                _configuration.GetSection("strategies-service"),
                configureAction: conf =>
                {
                    conf.StrategiesServiceName = instance.Name;
                    conf.AccountsServiceName = asName;
                    conf.Monolith = true;
                    conf.SingleHost = true;
                })
            .AddStrategiesCore()

            // health checks
            // .Configure<EventsProcessedConfig>(conf =>
            //     configuration.GetSection("health-checks").GetSection("events-processed").Bind(conf)
            // )
            // .AddSingleton<EventsProcessedConfig>(sp =>
            //     sp.GetService<IOptions<EventsProcessedConfig>>()?.Value ?? new())
            ;
        
        return sc.BuildServiceProvider();
    }

    private IServiceProvider SetupExecutionService(ExecutionServiceInstance instance)
    {
        var sp = new ServiceCollection()
            .AddLogging(c =>
            {
                c.ClearProviders();
                c.AddNLog();
                LogManager.Configuration = new NLogLoggingConfiguration(_configuration.GetSection("nlog"));
            })
            
            .ReuseInProcessMessaging(_serviceProvider)
            .AddSingleton<ISecretProvider>(_serviceProvider.GetRequiredService<ISecretProvider>())
            
            .ConfigureMainDb(_serviceProvider.GetRequiredService<NpgsqlDataSource>())
            .AddMainDbContext()
            .UseTradingAccountsRepositoryReadonly()
			
            .UseSingletonInMemoryBus()
			
            .AddQuartz()
            .AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = false;
            })
            
            .AddSingleton<ITypeResolver>(sp => new MultipleAssembliesTypeResolver(Array.Empty<string>()))
			
            .AddSingleton<IClock>(_ => SystemClock.Instance)
			
            .AddSingleton<AccountsServiceApiConfig>(new AccountsServiceApiConfig { AccountsServiceName = "AS", ClientName = instance.Name, })
            .AddSingleton<AccountsServiceApi>()
            .AddSingleton<IAccountsServiceApiReadonly>(sp => sp.GetRequiredService<AccountsServiceApi>())
            .AddSingleton<IAccountsServiceApi>(sp => sp.GetRequiredService<AccountsServiceApi>())
            .AddSingleton<Sender>(sp => sp.GetRequiredService<AccountsServiceApi>().Sender)
            .AddDealerMessageFactory(instance.Name)
			
            .ConfigureDisruptors(_configuration.GetSection("execution-service"))
            .ConfigureExecutionService(
                _configuration.GetSection("execution-service"),
                configureAction: conf =>
                {
                    conf.ExecutionServiceName = instance.Name;
                    conf.Monolith = true;
                    conf.SingleHost = true;
                })
            .AddExecutionService()
            .AddSingleton<IHostApplicationLifetime>(_serviceProvider.GetRequiredService<IHostApplicationLifetime>())
            .BuildServiceProvider();

        return sp;
    }

    private IServiceProvider SetupMarketDataService()
    {
        var transportFactory = _serviceProvider.GetRequiredService<TransportFactory>();
        
        var sp = new ServiceCollection()
            
            .AddLogging(c =>
            {
                c.ClearProviders();
                c.AddNLog();
                LogManager.Configuration = new NLogLoggingConfiguration(_configuration.GetSection("nlog"));
            })
            
            .AddQuartz()
            .AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = false;
            })
            
            .ConfigureDisruptors(_configuration.GetSection(MdsName))

            .AddSingleton<IClock>(sp => SystemClock.Instance)
            
            .AddJsonMessages("rabbitmq")
            .AddDefaultJsonSerializerSettings("rabbitmq")
            .AddJsonMessages()
            .AddDefaultJsonSerializerSettings()
            .AddSingleton<IMulticastMessageFactory, MulticastMessageFactory>()
            .AddSingleton<ITypeResolver>(_ => new MultipleAssembliesTypeResolver(Array.Empty<string>()))

            .ConfigureMainDb(_serviceProvider.GetRequiredService<NpgsqlDataSource>())
            .AddMainDbContext()
            .UseMainDbMarketDataServiceStreamsRepository()

            .ConfigureMarketDataHistoryDb(_serviceProvider.GetRequiredService<MDDatasource>())
            .AddMarketDataHistoryDbContext()
            .UseMarketDataPersisterDAL()
            
            .AddSingleton<ITransport<Common.Messaging.Patterns.TopicMulticast.DownstreamMessage>>(sp =>
                transportFactory.CreateMulticastTransport(MdsName, sp.GetRequiredService<Disruptor<IncomingDisruptorMessage>>()))

            .ConfigureMarketDataService(
                _configuration.GetSection("mds"),
                configureAction: conf =>
                {
                    conf.MarketDataServiceName = "MDS";
                    conf.SingleHost = true;
                    conf.Monolith = true;
                })
            .AddSimpleMarketDataService()
            
            .AddSingleton<IHostApplicationLifetime>(_serviceProvider.GetRequiredService<IHostApplicationLifetime>())
            .BuildServiceProvider();

        return sp;
    }

    private IServiceProvider SetupBinanceFuturesUsdmMarketDataClient(string clientName)
    {
        var transportFactory = _serviceProvider.GetRequiredService<TransportFactory>();
        
        var sp = new ServiceCollection()

            .AddLogging(c =>
            {
                c.ClearProviders();
                c.AddNLog();
                LogManager.Configuration = new NLogLoggingConfiguration(_configuration.GetSection("nlog"));
            })

            .AddQuartz()
            .AddQuartzHostedService()


            .AddJsonMessages("rabbitmq")
            .AddDefaultJsonSerializerSettings("rabbitmq")
            .AddJsonMessages()
            .AddDefaultJsonSerializerSettings()
            .AddSingleton<ITypeResolver>(sp => new MultipleAssembliesTypeResolver(Array.Empty<string>()))
            .AddSingleton<IMulticastMessageFactory, MulticastMessageFactory>()
            
            .ConfigureMainDb(_serviceProvider.GetRequiredService<NpgsqlDataSource>())
            .AddMainDbContext()
            .UseMainDbBinanceActiveSubscriptionsRepository()
            .UseMainDbBinanceOrderBookSubscriptionsRepository()
            .UseMainDbMarketDataServiceStreamsRepository()

            .ConfigureDisruptors(_configuration.GetSection(clientName))
            .AddInputDisruptor()
            .AddOutputDisruptor()

            .AddQuartz()
            .AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = false;
            })

            .AddSingleton<IClock>(_ => SystemClock.Instance)

            .ConfigureBinanceUsdmFuturesMarketDataGateway(
                _configuration.GetSection(clientName),
                configureAction: conf =>
                {
                    conf.ClientName = clientName;
                }
            )
            .AddBinanceUsdmFuturesMarketDataGatewayService()
            
            .ConfigureMarketDataHistoryDb(_serviceProvider.GetRequiredService<MDDatasource>())
            .AddMarketDataHistoryDbContext()
            .UseMarketDataPersisterDAL()
            
            .AddSingleton<ITransport<Common.Messaging.Patterns.TopicMulticast.DownstreamMessage>>(sp =>
                transportFactory.CreateMulticastTransport(clientName, sp.GetRequiredService<Disruptor<IncomingDisruptorMessage>>()))
            
            .ConfigureMarketDataService(
                _configuration.GetSection(clientName),
                configureAction: conf =>
                {
                    conf.MarketDataServiceName = clientName;
                    conf.SingleHost = true;
                    conf.Monolith = true;
                }
            )
            .AddEmbeddedMarketDataService(false)
            
            .AddSingleton<IHostApplicationLifetime>(_serviceProvider.GetRequiredService<IHostApplicationLifetime>())

            // health checks
            // .Configure<MarketDataUpdatedConfig>(conf =>
            //     configuration.GetSection("health-checks").GetSection("market-data-updated").Bind(conf)
            // )
            // .AddSingleton<MarketDataUpdatedConfig>(sp =>
            //     sp.GetService<IOptions<MarketDataUpdatedConfig>>()?.Value ?? new())
            
            .BuildServiceProvider();
        ;

        return sp;
    }
}