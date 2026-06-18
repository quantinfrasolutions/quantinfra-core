using Disruptor.Dsl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.ServiceBase.WAL;

namespace QuantInfra.Common.ServiceBase;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureWalManager(this IServiceCollection sc, IConfiguration configuration, string sectionName = "wal") => sc
        .Configure<WalConfig>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<WalConfig>(sp => sp.GetRequiredService<IOptions<WalConfig>>().Value);
    
    public static IServiceCollection AddWalManager<TState>(this IServiceCollection sc) where TState : class, IState<TState>, new()
        => sc
            .AddSingleton<WalManager<TState>>();

    public static IServiceCollection ConfigureDisruptors(this IServiceCollection sc, IConfiguration configuration, string sectionName = "disruptors") => sc
        .Configure<DisruptorConfig>(conf => configuration.GetSection(sectionName).Bind(conf))
        .AddSingleton<DisruptorConfig>(sp => sp.GetService<IOptions<DisruptorConfig>>()?.Value ?? new());
    
    public static IServiceCollection AddInputDisruptor(this IServiceCollection sc) =>
        sc.AddSingleton<Disruptor<IncomingDisruptorMessage>>(sp =>
        {
            var config = sp.GetRequiredService<DisruptorConfig>();
            return new Disruptor<IncomingDisruptorMessage>(() => new IncomingDisruptorMessage(),
                config.InputDisruptorRingBufferSize,
                config.GetWaitStrategy()
            );
        });
    
    public static IServiceCollection AddOutputDisruptor(this IServiceCollection sc) =>
        sc.AddSingleton<Disruptor<OutgoingDisruptorMessage>>(sp =>
        {
            var config = sp.GetRequiredService<DisruptorConfig>();
            return new Disruptor<OutgoingDisruptorMessage>(() => new OutgoingDisruptorMessage(),
                config.OutputDisruptorRingBufferSize, 
                config.GetWaitStrategy()
            );
        });

    public static IServiceCollection AddDisruptorAsyncQueryBus(this IServiceCollection sc) => sc
        .AddSingleton<DisruptorAsyncQueryBus>()
        .AddSingleton<AsyncQueryBus>(sp => sp.GetRequiredService<DisruptorAsyncQueryBus>());
}