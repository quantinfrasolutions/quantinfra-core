using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.Handlers;
using QuantInfra.Services.MarketData.Embedded;

namespace QuantInfra.Services.MarketData
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection ConfigureMarketDataService(this IServiceCollection sc,
            IConfiguration configuration, string sectionName = "mds",
            Action<Config>? configureAction = null
        ) => sc
            .Configure<Config>(conf => configuration.GetSection(sectionName).Bind(conf))
            .AddSingleton<Config>(sc =>
            {
                var config = sc.GetRequiredService<IOptions<Config>>().Value;
                configureAction?.Invoke(config);
                return config;
            });
        
        public static IServiceCollection AddSimpleMarketDataService(this IServiceCollection sc) => sc
            .AddSingleton<Bpl>()

            .AddInputDisruptor()
            .AddOutputDisruptor()
            
            .AddSingleton<ParserConfig>(sp =>
            {
                var config = sp.GetRequiredService<Config>();
                return new ParserConfig
                {
                    WritePerformanceMetrics = config.WritePerformanceMetrics,
                    ServiceName = config.MarketDataServiceName,
                    Monolith =  config.Monolith,
                };
            })
            .AddSingleton<Parser>(sp => new(sp.GetRequiredKeyedService<IMessageFactory>("rabbitmq"), sp.GetRequiredService<ParserConfig>()))
            .AddSingleton<IReceiverStateProvider, MockReceiverStateProvider>()
            
            .AddSingleton<MulticastSender>()

            .AddSingleton<Persister>()

            .AddSingleton<IRequestSnapshotMessageHandler, RequestSnapshotMessageHandler>()

            .AddSingleton<SimpleMarketDataService>()
            .AddSingleton<IHostedService>(sp => sp.GetRequiredService<SimpleMarketDataService>());
        
        
        public static IServiceCollection AddEmbeddedMarketDataService(this IServiceCollection sc, bool addHostedService = true)
        {
            sc
                .AddSingleton<Bpl>()
                
                .AddInputDisruptor()
                .AddOutputDisruptor()
                
                .AddSingleton<InputDisruptorPublisher>()
                .AddSingleton<IPublisherFactory>(sp => sp.GetRequiredService<InputDisruptorPublisher>())
                .AddSingleton<IReceiverStateProvider, MockReceiverStateProvider>()
                
                .AddSingleton<MulticastSender>()
                
                .AddSingleton<Persister>()
                
                .AddSingleton<IRequestSnapshotMessageHandler, RequestSnapshotMessageHandler>()
                
                .AddSingleton<EmbeddedMarketDataService>();
            
            if (addHostedService)
            {
                sc.AddHostedService(sp => sp.GetRequiredService<EmbeddedMarketDataService>());
            }
            return sc;
        }
        
        public static IServiceCollection AddMarketDataServiceComponents(this IServiceCollection sc) => sc
            .AddSingleton<Bpl>()
            
            .AddOutputDisruptor()
            
            .AddSingleton<MulticastSender>()
            
            .AddSingleton<IReceiverStateProvider, MockReceiverStateProvider>()
            
            .AddSingleton<Persister>();
    }
}
