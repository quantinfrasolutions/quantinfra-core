using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.InProcess;
using QuantInfra.Databases.Main;
using QuantInfra.Databases.MarketDataHistory;
using QuantInfra.Services.ManagementCore;
using QuantInfra.Services.MonolithService.Management;
using Quartz;

namespace QuantInfra.Services.MonolithService;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureMonolithService(this IServiceCollection sc, IConfiguration configuration, string sectionName = "app") => sc
        .Configure<Config>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<Config>(sp => sp.GetService<IOptions<Config>>()?.Value ?? new());
    
    public static IServiceCollection AddMonolithService(this IServiceCollection sc, IConfiguration configuration, bool addHostedService = true)
    {
        sc
            .ConfigureMainDb(configuration)
            .AddMainDbContext()
            .UseMainDbInfrastructureRepository()
            .UseMainDbAccountRecordsRepository()
            .UseMainDbStrategyRecordsRepository()
            .UseMainDbStaticDataRepositoryReadOnly()
            .UseTradingAccountsRepository()
            
            .AddSingleton<FileSecretProviderConfig>(sp => new() { FilePath = Path.Combine(sp.GetRequiredService<Config>().WorkingDirPath, ".secret") })
            .AddSingleton<ISecretProvider, FileSecretProvider>()
            
            .ConfigureMarketDataHistoryDb(configuration)
            .AddMarketDataHistoryDbContext()
            
            .ConfigureInProcessMessaging(configuration)
            .AddInProcessMessaging()
            .AddSingleton<Management.ManagementListener>()
            .AddHostedService(sp => sp.GetRequiredService<Management.ManagementListener>())

            .AddSingleton<IPublisherFactory, ManagementPublisherFactory>()
            .ConfigureManagementCore(configuration.GetSection("management-service"))
            .AddManagementCoreServices()
            .AddManagementServiceClient()
            
            .AddSingleton<IConfiguration>(configuration)
            
            .AddQuartz()
            .AddQuartzHostedService()
            
            .AddSingleton<IClock>(SystemClock.Instance)
            
            .AddSingleton<Service>()
            .AddSingleton<IHostedComponentsStatusProvider>(sp => sp.GetRequiredService<Service>());

        if (addHostedService) sc.AddHostedService(sp => sp.GetRequiredService<Service>());
        
        return sc;
    }
}