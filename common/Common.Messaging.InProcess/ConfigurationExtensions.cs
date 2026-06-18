using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuantInfra.Common.Messaging.InProcess.Messages.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.InProcess.Messages.TopicMulticast;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;

namespace QuantInfra.Common.Messaging.InProcess;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureInProcessMessaging(this IServiceCollection sc, IConfiguration configuration, string sectionName = "topology") => sc
        .Configure<TopologyConfig>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<TopologyConfig>(sp => sp.GetService<IOptions<TopologyConfig>>()?.Value ?? new());
    
    public static IServiceCollection AddInProcessMessaging(this IServiceCollection sc) => sc
        .AddSingleton<Topology>()
        .AddSingleton<IMessageFactory, MessageFactory>()
        .AddSingleton<IMulticastMessageFactory, MulticastMessageFactory>()
        .AddSingleton<TransportFactory>()
        .AddSingleton<TransportFactory>();
    
    public static IServiceCollection ReuseInProcessMessaging(this IServiceCollection sc, IServiceProvider sp) => sc
        .AddSingleton<Topology>(sp.GetRequiredService<Topology>())
        .AddSingleton<IMessageFactory>(sp.GetRequiredService<IMessageFactory>())
        .AddSingleton<IMulticastMessageFactory>(sp.GetRequiredService<IMulticastMessageFactory>())
        .AddSingleton<TransportFactory>(sp.GetRequiredService<TransportFactory>())
        .AddSingleton<TransportFactory>(sp.GetRequiredService<TransportFactory>());

    public static IServiceCollection AddDealerMessageFactory(this IServiceCollection sc, string clientName) => sc
        .AddSingleton<IDealerRouterMessageFactory, DealerRouterMessageFactory>(_ => new(clientName));
}