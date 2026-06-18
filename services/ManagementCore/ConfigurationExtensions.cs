using ManagementCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuantInfra.Common.Interfaces.Api.Management;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Connectors.Common;

namespace QuantInfra.Services.ManagementCore;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureManagementCore(this IServiceCollection sc, IConfiguration configuration, string sectionName = "management-service") => sc
        .Configure<Config>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<Config>(sp => sp.GetRequiredService<IOptions<Config>>().Value);
    
    /// <summary>
    /// Requires:
    /// IAccountRecordsRepository, IStrategyRecordsRepository, IPublisherFactory
    /// </summary>
    public static IServiceCollection AddManagementCoreServices(this IServiceCollection sc)
    {
        sc
            .AddSingleton<IPublisher>(sp => sp.GetRequiredService<IPublisherFactory>().GetPublisher("management"))
            .AddSingleton<RequestsManager<NewOrderIdentifier>>(sp => new(() => new(0, "")))
            .AddSingleton<RequestsManager<Guid>>(sp => new(Guid.NewGuid))
            .AddSingleton<AccountsServiceResponseListener>()
            .AddSingleton<IMulticastListenerEventHandler>(sp => sp.GetRequiredService<AccountsServiceResponseListener>())
            .AddScoped<ManagementService>();

        return sc;
    }

    public static IServiceCollection AddManagementServiceClient(this IServiceCollection sc) => sc
        .AddScoped<IManagementServiceClient, ManagementServiceClient>();
}